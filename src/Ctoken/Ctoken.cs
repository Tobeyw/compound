using Neo;

using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;
using System;

using System.ComponentModel;
using System.Numerics;

namespace Ctoken
{
    [ManifestExtra("Author", "Neo")]
    [ManifestExtra("Email", "dev@neo.org")]
    [ManifestExtra("Description", "This is Ctoken")]
    public partial class Ctoken : SmartContract
    {
        private static readonly BigInteger Ten2Power8 = 100000000; // price or amount decimal = 10^8
        private static readonly BigInteger Ten2Power18 = 1000000000000000000; // ratio decimal = 10 ^ 18
        [InitialValue("NX4pQCjXkJHMKzXw3ccVdEFw6SrgePNP6r", ContractParameterType.Hash160)]
        static readonly UInt160 Owner = default;
        public static int initialize(
                        uint initialExchangeRateMantissa_,
                        string name_,
                        string symbol_,
                        ulong decimals_,
                        UInt160 comptroller,
                        UInt160 InterestModel)
        {
            AdminSnapshot adminSnapshot = defaultAdmin.Get();
            AccountSnapshot accSnapshot = defaultMessage.Get();
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            if (sender != adminSnapshot.admin)
            {
                throw new Exception("only admin may initialize the market");
            }
            if (accSnapshot.accrualBlockNumber != 0 || accSnapshot.borrowIndex != 0)
            {
                throw new Exception("market may only be initialized once");
            }
            accSnapshot.initialExchangeRateMantissa = initialExchangeRateMantissa_;
            if (accSnapshot.initialExchangeRateMantissa <= 0)
            {
                throw new Exception("initial exchange rate must be greater than zero");
            }
            /*uint err = setComptroller(comptroller);
            if (err != (uint)Error.NO_ERROR)
            {
                throw new Exception("setting comptroller failed");
            }*/

            accSnapshot.accrualBlockNumber = getBlockNumber();
            accSnapshot.borrowIndex = mantissaOne;

            //err = _setInterestRateModelFresh(interestRateModel_);
            //if (err != (uint)Error.NO_ERROR)
            //{
            //    throw new Exception("setting interest rate model failed");
            //}

            accSnapshot.name = name_;
            accSnapshot.symbol = symbol_;
            accSnapshot.decimals = decimals_;
            accSnapshot._notEntered = true;
            accSnapshot.borrowRateMaxMantissa = 5_000_000_000_000;
            accSnapshot.reservesFactorMaxMantissa = 1_000_000_000_000_000_000;

            defaultMessage.Put(accSnapshot);
            return 0;
        }

        
        #region Notidicaitons

        [DisplayName("Transfer")]
        public static event Action<UInt160, UInt160, BigInteger> OnTransfer;

        [DisplayName("Approval")]
        public static event Action<UInt160, UInt160, BigInteger> OnApproval;

        [DisplayName("AccrueInterest")]
        public static event Action<BigInteger, BigInteger, BigInteger, BigInteger> OnAccrueInterest;

        [DisplayName("Mint")]
        public static event Action<UInt160, BigInteger, BigInteger> OnMint;

        [DisplayName("Redeem")]
        public static event Action<UInt160, ulong, ulong> OnRedeem;

        [DisplayName("Borrow")]
        public static event Action<UInt160, ulong, ulong, ulong> OnBorrow;

        [DisplayName("RepayBorrow")]
        public static event Action<UInt160, UInt160, ulong, ulong, ulong> OnRepayBorrow;

        [DisplayName("LiquidateBorrow")]
        public static event Action<UInt160, UInt160, ulong, UInt160, ulong> OnLiquidateBorrow;

        [DisplayName("NewPendingAdmin")]
        public static event Action<UInt160, UInt160> OnNewPendingAdmin;

        [DisplayName("NewAdmin")]
        public static event Action<UInt160, UInt160> OnNewAdmin;

        //[DisplayName("NewComptroller")]
        //public static event Action<ComptrollerInterface, ComptrollerInterface> OnNewComptroller;

        [DisplayName("ReservesAdded")]
        public static event Action<UInt160, ulong, ulong> OnReservesAdded;

        [DisplayName("ReserveseReduced")]
        public static event Action<UInt160, ulong, ulong> OnReservesReduced;

        [DisplayName("NewReserveFactor")]
        public static event Action<ulong, ulong> OnNewReserveFactor;

        [DisplayName("OnNewComptroller")]
        public static event Action<UInt160, UInt160> OnNewComptroller;

        //[DisplayName("NewMarketIntrerstRateModel")]
        //public static event Action<InterestRateModelInterface, InterestRateModelInterface> OnNewMarketInterestRateModel;
        #endregion

        public static Boolean isCToken() => true;
        



        public static BigInteger transferTokens(UInt160 spender, UInt160 src, UInt160 dst, BigInteger tokens)
        {
/*            uint allowed = (uint)comptroller.comptroller.transferAllowed(spender, src, dst, tokens);
            if (allowed != 0)
            {
                return (int)failOpaque(Error.COMPTROLLER_REJECTION, FailureInfo.TRANSFER_COMPTROLLER_REJECTION, (int)allowed);
            }
*/
            if (src == dst)
            {
                return (int)fail(Error.BAD_INPUT, FailureInfo.TRANSFER_NOT_ALLOWED);
            }

            BigInteger startingAllowance = 0;
            if (spender == src)
            {
                startingAllowance = -1;
            }
            else
            {
                Map<UInt160, BigInteger> map = transferAllowance.Get(src);
                startingAllowance = map[spender];
            }

            MathError mathErr;
            BigInteger allowanceNew;
            BigInteger srcTokensNew;
            BigInteger dstTokensNew;

            (mathErr, allowanceNew) = subUInt((uint)startingAllowance, tokens);
            if (mathErr != MathError.NO_ERROR)
            {
                return (int)fail(Error.MATH_ERROR, FailureInfo.TRANSFER_NOT_ALLOWED);
            }
            (mathErr, srcTokensNew) = subUInt((uint)accountTokens.Get(src), tokens);
            if (mathErr != MathError.NO_ERROR)
            {
                return (int)fail(Error.MATH_ERROR, FailureInfo.TRANSFER_NOT_ENOUGH);
            }
            (mathErr, dstTokensNew) = addUInt((uint)accountTokens.Get(dst), tokens);
            if (mathErr != MathError.NO_ERROR)
            {
                return (int)fail(Error.MATH_ERROR, FailureInfo.TRANSFER_TOO_MUCH);
            }

            accountTokens.Put(src, srcTokensNew);
            accountTokens.Put(dst, dstTokensNew);

            if (startingAllowance != -1)
            {
                Map<UInt160, BigInteger> getAccount = transferAllowance.Get(src);
                getAccount[spender] = (BigInteger)allowanceNew;
            }
            OnTransfer(src, dst, tokens);
            return (int)Error.NO_ERROR;
        }

        public static bool transfer(UInt160 dst, BigInteger amount)
        {
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            return transferTokens(sender, sender, dst, amount) == (int)Error.NO_ERROR;
        }

        public static bool transferFrom(UInt160 src, UInt160 dst, BigInteger amount)
        {
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            return transferTokens(sender, src, dst, amount) == (int)Error.NO_ERROR;
        }

        public static bool approve(UInt160 spender, BigInteger amount)
        {
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 src = tx.Sender;
            Map<UInt160, BigInteger> map = transferAllowance.Get(src);
            map[spender] = amount;
            OnApproval(src, spender, amount);
            return true;
        }

        public static BigInteger allowance(UInt160 owner, UInt160 spender)
        {
            Map<UInt160, BigInteger> map = transferAllowance.Get(owner);
            return map[spender];
        }

        public static BigInteger balanceOf(UInt160 owner)
        {
            return accountTokens.Get(owner);
        }

        public static BigInteger balanceOfUnderlying(UInt160 owner)
        {
            Exp exchangeRate = new Exp();
            //exchangeRate.mantissa = 200;
            exchangeRate.mantissa = exchangeRateCurrent();
            (MathError mErr, BigInteger balance) = mulScalarTruncate(exchangeRate, (ulong)accountTokens.Get(owner));
            if (mErr != MathError.NO_ERROR)
            {
                throw new Exception("balance could not be calculated");
            }
            return balance;
        }

        public static (uint, BigInteger, BigInteger, BigInteger) getAccountSnapshot(UInt160 account)
        {
            BigInteger cTokenBalance = accountTokens.Get(account);
            BigInteger borrowBalance;
            BigInteger exchangeRateMantissa;

            MathError mErr;
            (mErr, borrowBalance) = (MathError.NO_ERROR,200);
            //(mErr, borrowBalance) = borrowBalanceStoredInternal(account);
            if (mErr != MathError.NO_ERROR)
            {
                return ((uint)Error.MATH_ERROR, 0, 0, 0);
            }
            //(mErr, exchangeRateMantissa) = (MathError.NO_ERROR,200);
            (mErr, exchangeRateMantissa) = exchangeRateStoredInternal();
            if (mErr != MathError.NO_ERROR)
            {
                return ((uint)Error.MATH_ERROR, 0, 0, 0);
            }
            return ((uint)Error.NO_ERROR, cTokenBalance, borrowBalance, exchangeRateMantissa);
        }

        public static uint getBlockNumber()
        {
            Block block = new Block();
            return block.Index;
        }

        public static BigInteger borrowRatePerBlock()
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            return (BigInteger)getBorrowRate(getCashPrior(),accSnapshot.totalBorrows, accSnapshot.totalReserves);
        }

        public static BigInteger supplyRatePerBlock()
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            return (BigInteger)getSupplyRate(getCashPrior(), accSnapshot.totalBorrows, accSnapshot.totalReserves,accSnapshot.reservesFactorMantissa);
        }

        public static BigInteger totalBorrowCurrent()
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            /*if (accrueInterest() != (uint)Error.NO_ERROR)
            {
                throw new Exception("accrue interest failed");
            }*/
            return accSnapshot.totalBorrows;
        }

        public static BigInteger totalBorrowStored()
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            return accSnapshot.totalBorrows;
        }

        public static BigInteger borrowBalanceCurrent(UInt160 account)
        {
            /*            if (accrueInterest() != (uint)Error.NO_ERROR)
                        {
                            throw new Exception("accrue interest failed");
                        }*/

            (MathError err, BigInteger result) = borrowBalanceStoredInternal(account);

            if (err != MathError.NO_ERROR)
            {
                throw new Exception("borrowBalanceStored: borrowBalanceStoredInternal failed");
            }
            return result;
        }
        public static BigInteger borrowBalanceStored(UInt160 account)
        {
            (MathError err, BigInteger result) = borrowBalanceStoredInternal(account);

            if (err != MathError.NO_ERROR)
            {
                throw new Exception("borrowBalanceStored: borrowBalanceStoredInternal failed");
            }
            return result;

        }

        public static (MathError, BigInteger) borrowBalanceStoredInternal(UInt160 account)
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            MathError mathErr;
            BigInteger pricipalTimesIndex;
            BigInteger result;

            BorrowSnapshot borrowSnapshot = accountBorrows.Get(account);

            if (borrowSnapshot.principal == 0)
            {
                return (MathError.NO_ERROR, 0);
            }

            (mathErr, pricipalTimesIndex) = mulUInt(borrowSnapshot.principal, accSnapshot.borrowIndex);
            if (mathErr != MathError.NO_ERROR)
            {
                return (mathErr, 0);
            }

            (mathErr, result) = divUInt(pricipalTimesIndex, borrowSnapshot.interestIndex);
            if (mathErr != MathError.NO_ERROR)
            {
                return (mathErr, 0);
            }

            return (MathError.NO_ERROR, result);

        }

        public static BigInteger exchangeRateCurrent()
        {
/*            if (accrueInterest() != (uint)Error.NO_ERROR)
            {
                throw new Exception("accrue interest failed");
            }*/
            return exchangeRateStored();
        }

        public static BigInteger exchangeRateStored()
        {
            (MathError err, BigInteger result) = exchangeRateStoredInternal();
            if (err != MathError.NO_ERROR)
            {
                throw new Exception("exchangedRateStored:exchangeRateStoredInternal failed");
            }
            return result;
        }

        public static (MathError, BigInteger) exchangeRateStoredInternal()
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            BigInteger _totalSupply = accSnapshot.totalSupply;
            if (_totalSupply == 0)
            {
                return (MathError.NO_ERROR, accSnapshot.initialExchangeRateMantissa);
            }
            else
            {
                BigInteger totalCash = getCashPrior();
                BigInteger cashPlusBorrowMinusReserves;
                Exp exchangeRate;
                MathError mathErr;

                (mathErr, cashPlusBorrowMinusReserves) = addThenSubUInt(totalCash, accSnapshot.totalBorrows, accSnapshot.totalReserves);

                if (mathErr != MathError.NO_ERROR)
                {
                    return (mathErr, 0);
                }

                (mathErr, exchangeRate) = getExp(cashPlusBorrowMinusReserves, _totalSupply);
                if (mathErr != MathError.NO_ERROR)
                {
                    return (mathErr, 0);
                }
                return (MathError.NO_ERROR, exchangeRate.mantissa);

            }
        }



        public static BigInteger getCash()
        {
            return getCashPrior();
        }





 /*       public static uint accrueInterest()
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            uint currentBlockNumber = getBlockNumber();
            BigInteger accrualBlockNumberPrior = accSnapshot.accrualBlockNumber;

            if (accrualBlockNumberPrior == currentBlockNumber)
            {
                return (uint)Error.NO_ERROR;
            }

            BigInteger cashPrior = getCashPrior();
            BigInteger borrowPrior = accSnapshot.totalBorrows;
            BigInteger reservesPrior = accSnapshot.totalReserves;
            BigInteger borrowIndexPrior = accSnapshot.borrowIndex;

            BigInteger borrowRateMantissa = getBorrowRate(cashPrior, borrowPrior, reservesPrior);
            if (borrowRateMantissa > accSnapshot.borrowRateMaxMantissa)
            {
                throw new Exception("borrow rate is absurdly high");
            }
            (MathError mathErr, BigInteger blockDelta) = subUInt(currentBlockNumber, accrualBlockNumberPrior);
            if (mathErr != MathError.NO_ERROR)
            {
                throw new Exception("could not calculate block delta");
            }

            Exp simpleInterestFactor;
            BigInteger interestAccmulated;
            BigInteger totalBorrowsNew;
            BigInteger totalReservesNew;
            BigInteger borrowIndexNew;

            Exp exp = new Exp();
            exp.mantissa = borrowRateMantissa;
            (mathErr, simpleInterestFactor) = mulScalar(exp, blockDelta);
            if (mathErr != MathError.NO_ERROR)
            {
                return (uint)failOpaque(Error.MATH_ERROR, FailureInfo.ACCRUE_INTEREST_SIMPLE_INTEREST_FACTOR_CALCULATION_FAILED, (int)mathErr);
            }
            (mathErr, interestAccmulated) = mulScalarTruncate(simpleInterestFactor, borrowPrior);

            if (mathErr != MathError.NO_ERROR)
            {
                return (uint)failOpaque(Error.MATH_ERROR, FailureInfo.ACCRUE_INTEREST_ACCUMULATED_INTEREST_CALCULATION_FAILED, (int)mathErr);
            }
            (mathErr, totalBorrowsNew) = addUInt(interestAccmulated, borrowPrior);
            if (mathErr != MathError.NO_ERROR)
            {
                return (uint)failOpaque(Error.MATH_ERROR, FailureInfo.ACCRUE_INTEREST_NEW_TOTAL_BORROWS_CALCULATION_FAILED, (int)mathErr);
            }
            Exp exp1 = new Exp();
            exp1.mantissa = accSnapshot.reservesFactorMantissa;
            (mathErr, totalReservesNew) = mulScalarTruncateAddUInt(exp1, interestAccmulated, reservesPrior); ;
            if (mathErr != MathError.NO_ERROR)
            {
                return (uint)failOpaque(Error.MATH_ERROR, FailureInfo.ACCRUE_INTEREST_NEW_TOTAL_RESERVES_CALCULATION_FAILED, (int)mathErr);
            }
            (mathErr, borrowIndexNew) = mulScalarTruncateAddUInt(simpleInterestFactor, borrowIndexPrior, borrowIndexPrior);
            if (mathErr != MathError.NO_ERROR)
            {
                return (uint)failOpaque(Error.MATH_ERROR, FailureInfo.ACCRUE_INTEREST_NEW_BORROW_INDEX_CALCULATION_FAILED, (int)mathErr);
            }

            accSnapshot.accrualBlockNumber = currentBlockNumber;
            accSnapshot.borrowIndex = borrowIndexNew;
            accSnapshot.totalBorrows = totalBorrowsNew;
            accSnapshot.totalReserves = totalReservesNew;
            defaultMessage.Put(accSnapshot);

            OnAccrueInterest(cashPrior, interestAccmulated, borrowIndexNew, totalBorrowsNew);
            return (uint)Error.NO_ERROR;
        }

        public static (uint, BigInteger) mintInternal(BigInteger mintAmount)
        {
            uint error = accrueInterest();
            if (error != (uint)(Error.NO_ERROR))
            {
                return (fail((Error)error, FailureInfo.MINT_ACCRUE_INTEREST_FAILED), 0);
            }
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            return mintFresh(sender, mintAmount);
        }

        public static (uint, BigInteger) mintFresh(UInt160 minter, BigInteger mintAmount)
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            UInt160 theToken = Runtime.ExecutingScriptHash;
            UInt160 comptroller = getComptroller();
            Object allowedObj = Contract.Call(comptroller, "mintAllowed", CallFlags.All, new object[] { theToken,minter,mintAmount });
            int allowed = (int)allowedObj;
            if (allowed != 0)
            {
                return (failOpaque(Error.COMPTROLLER_REJECTION, FailureInfo.MINT_COMPTROLLER_REJECTION, (int)allowed), 0);
            }

            if (accSnapshot.accrualBlockNumber != getBlockNumber())
            {
                return (fail(Error.MARKET_NOT_FRESH, FailureInfo.MINT_FRESHNESS_CHECK), 0);
            }
            MathError mathErr;
            BigInteger exchangeRateMantissa;
            BigInteger mintTokens;
            BigInteger totalSupplyNew;
            BigInteger accountTokensNew;
            BigInteger actualMintAmount;
            //(mathErr, exchangeRateMantissa) = (MathError.NO_ERROR,200);
            (mathErr, exchangeRateMantissa) = exchangeRateStoredInternal();

            if (mathErr != MathError.NO_ERROR)
            {
                return (failOpaque(Error.MATH_ERROR, FailureInfo.MINT_EXCHANGE_RATE_READ_FAILED, (int)mathErr), 0);
            }

            actualMintAmount = doTransferIn(minter, mintAmount);
            Exp exp = new Exp();
            exp.mantissa = exchangeRateMantissa;
            BigInteger mintTokensReplace;
            (mathErr, mintTokensReplace) = divScalarByExpTruncate(actualMintAmount, exp);
            mintTokens = (uint)mintTokensReplace;
            if (mathErr != MathError.NO_ERROR)
            {
                throw new Exception("MINT_EXCHANGE_CALCULATION_FAILED");
            }

            BigInteger totalSupplyNewReplace;
            (mathErr, totalSupplyNewReplace) = addUInt(accSnapshot.totalSupply, mintTokens);
            totalSupplyNew = totalSupplyNewReplace;
            if (mathErr != MathError.NO_ERROR)
            {
                throw new Exception("MINT_NEW_TOTAL_SUPPLY_CALCULATION_FAILED");
            }

            BigInteger accountTokensNewRepalce;
            (mathErr, accountTokensNewRepalce) = addUInt((ulong)accountTokens.Get(minter), mintTokens);
            accountTokensNew = accountTokensNewRepalce;
            if (mathErr != MathError.NO_ERROR)
            {
                throw new Exception("MINT_NEW_ACCOUNT_BALANCE_CALCULATION_FAILED");
            }

            accSnapshot.totalSupply = totalSupplyNew;
            accountTokens.Put(minter, accountTokensNew);
            accountTokens.Increase(Runtime.EntryScriptHash, mintAmount);
            OnMint(minter, actualMintAmount, mintTokens);
            OnTransfer(Runtime.ExecutingScriptHash, minter, mintTokens);

            return ((uint)Error.NO_ERROR, actualMintAmount);
        }*/

        /*public static uint redeemInternal(uint redeeemTokens)
        {
            uint error = accrueInterest();
            if (error != (uint)Error.NO_ERROR)
            {
                return fail((Error)error, FailureInfo.REDEEM_ACCRUE_INTEREST_FAILED);
            }
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            return redeemFresh(sender, redeeemTokens, 0);
        }

        public static uint redeemUnderlyingInternal(uint redeemAmount)
        {
            uint error = accrueInterest();
            if (error != (uint)Error.NO_ERROR)
            {
                return fail((Error)error, FailureInfo.REDEEM_ACCRUE_INTEREST_FAILED);
            }
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            return redeemFresh(sender, 0, redeemAmount);
        }

        public struct RedeemLocalVars
        {
            public Error error;
            public MathError mathErr;
            public uint exchangeRateMantissa;
            public uint redeemTokens;
            public uint redeemAmount;
            public uint totalSupplyNew;
            public uint accountTokensNew;
        }

        public static uint redeemFresh(UInt160 redeemer, uint redeemTokensIn, uint redeemAmountIn)
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            if (redeemTokensIn != 0 && redeemAmountIn != 0)
            {
                throw new Exception("one of redeemTokensIn or redeemAmountIn must ve zero");
            }

            RedeemLocalVars vars;

            (vars.mathErr, vars.exchangeRateMantissa) = exchangeRateStoredInternal();
            if (vars.mathErr != MathError.NO_ERROR)
            {
                return failOpaque(Error.MATH_ERROR, FailureInfo.REDEEM_EXCHANGE_RATE_READ_FAILED, (int)vars.mathErr);
            }

            if (redeemTokensIn > 0)
            {
                vars.redeemTokens = redeemTokensIn;
                Exp exp = new Exp();
                exp.mantissa = vars.exchangeRateMantissa;
                ulong redeemAmountReplace;
                (vars.mathErr, redeemAmountReplace) = mulScalarTruncate(exp, redeemTokensIn);
                vars.redeemAmount = (uint)redeemAmountReplace;
                if (vars.mathErr != MathError.NO_ERROR)
                {
                    return failOpaque(Error.MATH_ERROR, FailureInfo.REDEEM_EXCHANGE_TOKENS_CALCULATION_FAILED, (int)vars.mathErr);
                }
            }
            else
            {
                Exp exp1 = new Exp();
                exp1.mantissa = vars.exchangeRateMantissa;
                ulong redeemTokensReplace;
                (vars.mathErr, redeemTokensReplace) = divScalarByExpTruncate(redeemAmountIn, exp1);
                vars.redeemTokens = (uint)redeemTokensReplace;
                if (vars.mathErr != MathError.NO_ERROR)
                {
                    return failOpaque(Error.MATH_ERROR, FailureInfo.REDEEM_EXCHANGE_AMOUNT_CALCULATION_FAILED, (int)vars.mathErr);
                }
                vars.redeemAmount = redeemAmountIn;
            }

            uint allowed = (uint)comptroller.comptroller.redeemAllowed(Runtime.ExecutingScriptHash, redeemer, vars.redeemTokens);
            if (allowed != 0)
            {
                return failOpaque(Error.COMPTROLLER_REJECTION, FailureInfo.REDEEM_COMPTROLLER_REJECTION, (int)allowed);
            }
            if (accSnapshot.accrualBlockNumber != getBlockNumber())
            {
                return fail(Error.MARKET_NOT_FRESH, FailureInfo.REDEEM_FRESHNESS_CHECK);
            }

            ulong totalSupplyNewReplace;
            (vars.mathErr, totalSupplyNewReplace) = subUInt(accSnapshot.totalSupply, vars.redeemTokens);
            vars.totalSupplyNew = (uint)totalSupplyNewReplace;
            if (vars.mathErr != MathError.NO_ERROR)
            {
                return failOpaque(Error.MATH_ERROR, FailureInfo.REDEEM_NEW_TOTAL_SUPPLY_CALCULATION_FAILED, (int)vars.mathErr);
            }

            ulong accountTokensNewReplace;
            (vars.mathErr, accountTokensNewReplace) = subUInt((ulong)accountTokens.Get(redeemer), vars.redeemTokens);
            vars.accountTokensNew = (uint)accountTokensNewReplace;
            if (vars.mathErr != MathError.NO_ERROR)
            {
                return failOpaque(Error.MATH_ERROR, FailureInfo.REDEEM_NEW_ACCOUNT_BALANCE_CALCULATION_FAILED, (int)vars.mathErr);
            }

            if (getCashPrior() < vars.redeemAmount)
            {
                return fail(Error.TOKEN_INSUFFICIENT_CASH, FailureInfo.REDEEM_TRANSFER_OUT_NOT_POSSIBLE);
            }

            doTransferOut(redeemer, vars.redeemAmount);

            accSnapshot.totalSupply = vars.totalSupplyNew;
            accountTokens.Put(redeemer, vars.accountTokensNew);
            accountTokens.Reduce(Runtime.EntryScriptHash, redeemAmountIn);   
            OnTransfer(redeemer, Runtime.ExecutingScriptHash, vars.redeemTokens);
            OnRedeem(redeemer, vars.redeemAmount, vars.redeemTokens);

            return (uint)Error.NO_ERROR;
        }

        public static uint borrowInternal(uint borrowAmount)
        {
            uint error = accrueInterest();
            if (error != (uint)Error.NO_ERROR)
            {
                return fail((Error)error, FailureInfo.BORROW_ACCRUE_INTEREST_FAILED);
            }
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            return borrowFresh(sender, borrowAmount);
        }

        public struct BorrowLocalVars
        {
            public MathError mathErr;
            public uint accountBorrows;
            public uint accountBorrowsNew;
            public uint totalBorrowsNew;
        }

        public static uint borrowFresh(UInt160 borrower, uint borrowAmount)
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            uint allowed = (uint)comptroller.comptroller.borrowAllowed(Runtime.ExecutingScriptHash, borrower, borrowAmount);
            if (allowed != 0)
            {
                return failOpaque(Error.COMPTROLLER_REJECTION, FailureInfo.BORROW_COMPTROLLER_REJECTION, (int)allowed);
            }

            if (accSnapshot.accrualBlockNumber != getBlockNumber())
            {
                return fail(Error.MARKET_NOT_FRESH, FailureInfo.BORROW_FRESHNESS_CHECK);
            }

            if (getCashPrior() < borrowAmount)
            {
                return fail(Error.TOKEN_INSUFFICIENT_CASH, FailureInfo.BORROW_CASH_NOT_AVAILABLE);
            }

            BorrowLocalVars vars;

            (vars.mathErr, vars.accountBorrows) = borrowBalanceStoredInternal(borrower);
            if (vars.mathErr != MathError.NO_ERROR)
            {
                return failOpaque(Error.MATH_ERROR, FailureInfo.BORROW_ACCUMULATED_BALANCE_CALCULATION_FAILED, (int)vars.mathErr);
            }

            ulong accountBorrowsNewReplace;
            (vars.mathErr, accountBorrowsNewReplace) = addUInt(vars.accountBorrows, borrowAmount);
            vars.accountBorrowsNew = (uint)accountBorrowsNewReplace;
            if (vars.mathErr != MathError.NO_ERROR)
            {
                return failOpaque(Error.MATH_ERROR, FailureInfo.BORROW_NEW_ACCOUNT_BORROW_BALANCE_CALCULATION_FAILED, (int)vars.mathErr);
            }

            ulong totalBorrowsNewReplace;
            (vars.mathErr, totalBorrowsNewReplace) = addUInt(accSnapshot.totalBorrows, borrowAmount);
            vars.totalBorrowsNew = (uint)totalBorrowsNewReplace;
            if (vars.mathErr != MathError.NO_ERROR)
            {
                return failOpaque(Error.MATH_ERROR, FailureInfo.BORROW_NEW_TOTAL_BALANCE_CALCULATION_FAILED, (int)vars.mathErr);
            }

            doTransferOut(borrower, borrowAmount);

            BorrowSnapshot snapshot = accountBorrows.Get(borrower);
            snapshot.principal = vars.accountBorrowsNew;
            snapshot.interestIndex = (uint)accSnapshot.borrowIndex;
            accSnapshot.totalBorrows = vars.totalBorrowsNew;

            OnBorrow(borrower, borrowAmount, vars.accountBorrowsNew, vars.totalBorrowsNew);
            return (uint)Error.NO_ERROR;
        }

        public static (uint, uint) repayBorrowInternal(uint repayAmount)
        {
            uint error = accrueInterest();
            if (error != (uint)Error.NO_ERROR)
            {
                return (fail((Error)error, FailureInfo.REPAY_BORROW_ACCRUE_INTEREST_FAILED), 0);
            }
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            return repayBorrowFresh(sender, sender, repayAmount);
        }

        public static (uint, uint) repayBorrowBehalfInternal(UInt160 borrower, uint repayAmount)
        {
            uint error = accrueInterest();
            if (error != (uint)Error.NO_ERROR)
            {
                return (fail((Error)error, FailureInfo.REPAY_BEHALF_ACCRUE_INTEREST_FAILED), 0);
            }
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            return repayBorrowFresh(sender, borrower, repayAmount);
        }

        public struct RepayBorrowLocalVars
        {
            public Error err;
            public MathError mathErr;
            public uint repayAmount;
            public uint borrowerIndex;
            public uint accountBorrows;
            public uint accountBorrowsNew;
            public uint totalBorrowsNew;
            public uint actualRepayAmount;
        }

        public static (uint, uint) repayBorrowFresh(UInt160 payer, UInt160 borrower, uint repayAmount)
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            uint allowed = (uint)comptroller.comptroller.repayBorrowAllowed(Runtime.ExecutingScriptHash, payer, borrower, repayAmount);
            if (allowed != 0)
            {
                return (failOpaque(Error.COMPTROLLER_REJECTION, FailureInfo.REPAY_BORROW_COMPTROLLER_REJECTION, (int)allowed), 0);
            }

            if (accSnapshot.accrualBlockNumber != getBlockNumber())
            {
                return (fail(Error.MARKET_NOT_FRESH, FailureInfo.REPAY_BORROW_FRESHNESS_CHECK), 0);
            }

            RepayBorrowLocalVars vars;
            BorrowSnapshot snapshot = accountBorrows.Get(borrower);
            vars.borrowerIndex = snapshot.interestIndex;
            (vars.mathErr, vars.accountBorrows) = borrowBalanceStoredInternal(borrower);
            if (vars.mathErr != MathError.NO_ERROR)
            {
                return (failOpaque(Error.MATH_ERROR, FailureInfo.REPAY_BORROW_ACCUMULATED_BALANCE_CALCULATION_FAILED, (int)vars.mathErr), 0);
            }

            if ((int)repayAmount == -1)
            {
                vars.repayAmount = vars.accountBorrows;
            }
            else
            {
                vars.repayAmount = repayAmount;
            }

            vars.actualRepayAmount = doTransferIn(payer, vars.repayAmount);

            ulong accountBorrowsNewReplace;
            (vars.mathErr, accountBorrowsNewReplace) = subUInt(vars.accountBorrows, vars.actualRepayAmount);
            vars.accountBorrowsNew = (uint)accountBorrowsNewReplace;
            if (vars.mathErr != MathError.NO_ERROR)
            {
                throw new Exception("REPAY_BORROW_NEW_ACCOUNT_BORROW_BALANCE_CALCULATION_FAILED");
            }

            ulong totalBorrowsNewReplace;
            (vars.mathErr, totalBorrowsNewReplace) = subUInt(accSnapshot.totalBorrows, vars.actualRepayAmount);
            vars.totalBorrowsNew = (uint)totalBorrowsNewReplace;
            if (vars.mathErr != MathError.NO_ERROR)
            {
                throw new Exception("REPAY_BORROW_NEW_TOTAL_BALANCE_CALCULATION_FAILED");
            }

            BorrowSnapshot borrowerSnapshot = accountBorrows.Get(borrower);
            borrowerSnapshot.principal = vars.accountBorrowsNew;
            borrowerSnapshot.interestIndex = (uint)accSnapshot.borrowIndex;
            accSnapshot.totalBorrows = vars.totalBorrowsNew;

            OnRepayBorrow(payer, borrower, vars.actualRepayAmount, vars.accountBorrowsNew, vars.totalBorrowsNew);

            return ((uint)Error.NO_ERROR, vars.actualRepayAmount);
        }

        public static (uint, uint) liquidateBorrowInternal(UInt160 borrower, uint repayAmount, CtokenInterface cTokenCollateral)
        {
            uint error = accrueInterest();
            if (error != (uint)Error.NO_ERROR)
            {
                return (fail((Error)error, FailureInfo.LIQUIDATE_ACCRUE_BORROW_INTEREST_FAILED), 0);
            }

            error = (uint)accrueInterest();
            if (error != (uint)Error.NO_ERROR)
            {
                return (fail((Error)error, FailureInfo.LIQUIDATE_ACCRUE_COLLATERAL_INTEREST_FAILED), 0);
            }

            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            return liquidateBorrowFresh(sender, borrower, repayAmount, cTokenCollateral);
        }

        public static (uint, uint) liquidateBorrowFresh(UInt160 liquidator, UInt160 borrower, uint repayAmount, CtokenInterface cTokenCollateral)
        {
            uint allowed = liquidateBorrowAllowed(Runtime.ExecutingScriptHash, cTokenCollateral, liquidator, borrower, repayAmount);
            if (allowed != 0)
            {
                return (failOpaque(Error.COMPTROLLER_REJECTION, FailureInfo.LIQUIDATE_COMPTROLLER_REJECTION, (int)allowed), 0);
            }

            if (accrualBlockNumber != getBlockNumber())
            {
                return (fail(Error.MARKET_NOT_FRESH, FailureInfo.LIQUIDATE_FRESHNESS_CHECK), 0);
            }

            if (borrower == liquidator)
            {
                return (fail(Error.INVALID_ACCOUNT_PAIR, FailureInfo.LIQUIDATE_LIQUIDATOR_IS_BORROWER), 0);
            }

            if (repayAmount == 0)
            {
                return (fail(Error.INVALID_CLOSE_AMOUNT_REQUESTED, FailureInfo.LIQUIDATE_CLOSE_AMOUNT_IS_ZERO), 0);
            }

            if ((int)repayAmount == -1)
            {
                return (fail(Error.INVALID_CLOSE_AMOUNT_REQUESTED, FailureInfo.LIQUIDATE_CLOSE_AMOUNT_IS_UINT_MAX), 0);
            }

            (uint repayBorrowError, uint actualRepayAmount) = repayBorrowFresh(liquidator, borrower, repayAmount);
            if (repayBorrowError != (uint)Error.NO_ERROR)
            {
                return (fail((Error)repayBorrowError, FailureInfo.LIQUIDATE_REPAY_BORROW_FRESH_FAILED), 0);
            }

            (uint amountSeizeError, uint seizeTokens) = Comptroller.liquidateCalculateSeizeTokens(Runtime.ExecutingScriptHash, cTokenCollateral, actualRepayAmount);
            if (amountSeizeError != (uint)Error.NO_ERROR)
            {
                throw new Exception("LIQUIDATE_COMPTROLLER_CALCULATE_AMOUNT_SEIZE_FAILED");
            }
            if (balanceOf(borrower) < seizeTokens)
            {
                throw new Exception("LIQUIDATE_SEIZE_TOO_MUCH");
            }

            uint seizeError;
            if (cTokenCollateral == Runtime.ExecutingScriptHash)
            {
                seizeError = seizeInternal(Runtime.ExecutingScriptHash, liquidator, borrower, seizeTokens);
            }
            else
            {
                seizeError = seize(liquidator, borrower, seizeTokens);
            }

            if (seizeError != (uint)Error.NO_ERROR)
            {
                throw new Exception("token seizure failed");
            }
            OnLiquidateBorrow(liquidator, borrower, actualRepayAmount, cTokenCollateral, seizeTokens);

            return ((uint)Error.NO_ERROR, actualRepayAmount);
        }

        public static uint seize(UInt160 liquidator, UInt160 borrower, uint seizeTokens)
        {
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            return seizeInternal(sender, liquidator, borrower, seizeTokens);
        }

        public static uint seizeInternal(UInt160 seizerToken, UInt160 liquidator, UInt160 borrower, uint seizeTokens)
        {
            uint allowed = seizeAllowed(Runtime.ExecutingScriptHash, seizerToken, liquidator, borrower, seizeTokens);
            if (allowed != 0)
            {
                return failOpaque(Error.COMPTROLLER_REJECTION, FailureInfo.LIQUIDATE_SEIZE_COMPTROLLER_REJECTION, (int)allowed);
            }
            if (borrower == liquidator)
            {
                return fail(Error.INVALID_ACCOUNT_PAIR, FailureInfo.LIQUIDATE_SEIZE_LIQUIDATOR_IS_BORROWER);
            }

            MathError mathErr;
            ulong borrowerTokensNew;
            ulong liquidatorTokensNew;

            (mathErr, borrowerTokensNew) = subUInt((ulong)accountTokens.Get(borrower), seizeTokens);
            if (mathErr != MathError.NO_ERROR)
            {
                return failOpaque(Error.MATH_ERROR, FailureInfo.LIQUIDATE_SEIZE_BALANCE_DECREMENT_FAILED, (int)mathErr);
            }

            (mathErr, liquidatorTokensNew) = addUInt((ulong)accountTokens.Get(liquidator), seizeTokens);
            if (mathErr != MathError.NO_ERROR)
            {
                return failOpaque(Error.MATH_ERROR, FailureInfo.LIQUIDATE_SEIZE_BALANCE_INCREMENT_FAILED, (int)mathErr);
            }

            accountTokens.Put(borrower, borrowerTokensNew);
            accountTokens.Put(liquidator, liquidatorTokensNew);

            OnTransfer(borrower, liquidator, seizeTokens);

            return (uint)Error.NO_ERROR;
        }

        public static uint setPendingAdmin(UInt160 newPendingAdmin)
        {
            AdminSnapshot adminSnapshot = defaultAdmin.Get();
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            if (sender != adminSnapshot.admin)
            {
                return fail(Error.UNAUTHORIZED, FailureInfo.SET_PENDING_ADMIN_OWNER_CHECK);
            }

            UInt160 oldPendingAdmin = newPendingAdmin;
            adminSnapshot.pendingAdmin = newPendingAdmin;
            OnNewPendingAdmin(oldPendingAdmin, newPendingAdmin);
            defaultAdmin.Put(adminSnapshot);

            return (uint)Error.NO_ERROR;
        }

        public static uint acceptAdmin()
        {
            AdminSnapshot adminSnapshot = defaultAdmin.Get();
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            if (sender != adminSnapshot.pendingAdmin)
            {
                return fail(Error.UNAUTHORIZED, FailureInfo.ACCEPT_ADMIN_PENDING_ADMIN_CHECK);
            }

            UInt160 oldAdmin = adminSnapshot.admin;
            UInt160 oldPendingAdmin = adminSnapshot.pendingAdmin;

            adminSnapshot.admin = adminSnapshot.pendingAdmin;

            //adminSnapshot.pendingAdmin = X;

            OnNewAdmin(oldAdmin, adminSnapshot.admin);
            OnNewPendingAdmin(oldPendingAdmin, adminSnapshot.pendingAdmin);

            defaultAdmin.Put(adminSnapshot);
            return (uint)Error.NO_ERROR;
        }

        

        public static uint setReserveFactor(uint newReserveFactorMantissa)
        {
            uint error = accrueInterest();
            if (error != (uint)Error.NO_ERROR)
            {
                return fail((Error)error, FailureInfo.SET_RESERVE_FACTOR_ACCRUE_INTEREST_FAILED);
            }
            return setReserveFactorFresh(newReserveFactorMantissa);
        }

        public static uint setReserveFactorFresh(uint newReserveFactorMantissa)
        {
            AdminSnapshot adminSnapshot = defaultAdmin.Get();
            AccountSnapshot accSnapshot = defaultMessage.Get();
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            if (sender != adminSnapshot.admin)
            {
                return fail(Error.UNAUTHORIZED, FailureInfo.SET_RESERVE_FACTOR_ADMIN_CHECK);
            }

            if (accSnapshot.accrualBlockNumber != getBlockNumber())
            {
                return fail(Error.MARKET_NOT_FRESH, FailureInfo.SET_RESERVE_FACTOR_FRESH_CHECK);
            }

            if (newReserveFactorMantissa > accSnapshot.reservesFactorMaxMantissa)
            {
                return fail(Error.BAD_INPUT, FailureInfo.SET_RESERVE_FACTOR_BOUNDS_CHECK);
            }

            uint oldReserveFactorMantissa = (uint)accSnapshot.reservesFactorMantissa;
            accSnapshot.reservesFactorMantissa = newReserveFactorMantissa;

            OnNewReserveFactor(oldReserveFactorMantissa, newReserveFactorMantissa);

            return (uint)Error.NO_ERROR;
        }

        public static uint addReservesInternal(uint addAmount)
        {
            uint error = accrueInterest();
            uint actualAddAmount;
            if (error != (uint)Error.NO_ERROR)
            {
                return fail((Error)error, FailureInfo.ADD_RESERVES_ACCRUE_INTEREST_FAILED);
            }
            (error, actualAddAmount) = addReservesFresh(addAmount);
            return error;
        }

        public static (uint, uint) addReservesFresh(uint addAmount)
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            uint totalReservesNew;
            uint actualAddAmount;

            if (accSnapshot.accrualBlockNumber != getBlockNumber())
            {
                return (fail(Error.MARKET_NOT_FRESH, FailureInfo.ADD_RESERVES_FRESH_CHECK),0);
            }

            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            actualAddAmount = doTransferIn(sender, addAmount);
            totalReservesNew = (uint)accSnapshot.totalReserves + actualAddAmount;

            if (totalReservesNew < accSnapshot.totalReserves)
            {
                throw new Exception("add reserves unexpected overflow");
            }

            accSnapshot.totalReserves = totalReservesNew;

            OnReservesAdded(sender, actualAddAmount, totalReservesNew);

            return ((uint)Error.NO_ERROR, actualAddAmount);
        }
        
        public static uint reduceReserves(uint reduceAmount)
        {
            uint error = accrueInterest();
            if (error != (uint)Error.NO_ERROR)
            {
                return fail((Error)error, FailureInfo.REDUCE_RESERVES_ACCRUE_INTEREST_FAILED);
            }
            return reduceReservesFresh(reduceAmount);
        }

        public static uint reduceReservesFresh(uint reduceAmount)
        {
            AccountSnapshot accSnapshot = defaultMessage.Get();
            AdminSnapshot adminSnapshot = defaultAdmin.Get();
            uint totalReservesNew;
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;

            if (sender != adminSnapshot.admin)
            {
                return fail(Error.UNAUTHORIZED, FailureInfo.REDUCE_RESERVES_ADMIN_CHECK);
            }

            if (accSnapshot.accrualBlockNumber != getBlockNumber())
            {
                return fail(Error.MARKET_NOT_FRESH, FailureInfo.REDUCE_RESERVES_FRESH_CHECK);
            }

            if (getCashPrior() < reduceAmount)
            {
                return fail(Error.TOKEN_INSUFFICIENT_CASH, FailureInfo.REDUCE_RESERVES_CASH_NOT_AVAILABLE);
            }

            if (reduceAmount > accSnapshot.totalReserves)
            {
                return fail(Error.BAD_INPUT, FailureInfo.REDUCE_RESERVES_VALIDATION);
            }

            totalReservesNew = (uint)accSnapshot.totalReserves - reduceAmount;
            if (totalReservesNew > accSnapshot.totalReserves)
            {
                throw new Exception("reduce reserves unexpected underflow");
            }

            accSnapshot.totalReserves = totalReservesNew;

            doTransferOut(adminSnapshot.admin, reduceAmount);

            OnReservesReduced(adminSnapshot.admin, reduceAmount, totalReservesNew);

            defaultMessage.Put(accSnapshot);
            return (uint)Error.NO_ERROR;
        }

        public static uint setInterestRateModel()
        {
            uint error = accrueInterest();
            if (error != (uint)Error.NO_ERROR)
            {
                return fail((Error)error, FailureInfo.SET_INTEREST_RATE_MODEL_ACCRUE_INTEREST_FAILED);
            }
            return setInterestRateModelFresh();
        }

        public static uint setInterestRateModelFresh()
        {
            AdminSnapshot adminSnapshot = defaultAdmin.Get();
            AccountSnapshot accSnapshot = defaultMessage.Get();
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;

            if (sender != adminSnapshot.admin)
            {
                return fail(Error.UNAUTHORIZED, FailureInfo.SET_INTEREST_RATE_MODEL_OWNER_CHECK);
            }

            if (accSnapshot.accrualBlockNumber != getBlockNumber())
            {
                return fail(Error.MARKET_NOT_FRESH, FailureInfo.SET_INTEREST_RATE_MODEL_FRESH_CHECK);
            }

            //oldInterestRateModel = interestRateModel;
            //if (!newInterestRateModel.isInterestRateModel())
            //{
            //    throw new Exception("maker method returned false");
            //}

            //interestRateModel = newInterestRateModel;

            //OnNewMarketInterestRateModel(oldInterestRateModel, newInterestRateModel);

            return (uint)Error.NO_ERROR;
        }
*/

        public static uint setComptroller(UInt160 comptroller)
        {
            AdminSnapshot adminSnapshot = defaultAdmin.Get();
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;

            if (sender != adminSnapshot.admin)
            {
                return fail(Error.UNAUTHORIZED, FailureInfo.SET_COMPTROLLER_OWNER_CHECK);
            }

            Object isComptrollerObj = Contract.Call(comptroller, "isComptroller", CallFlags.All, new object[] {});
            Boolean isComptroller = (Boolean)isComptrollerObj;
            if (!isComptroller)
            {
                throw new Exception("marker method returned false");
            }

            Storage.Put(Storage.CurrentContext, "comptroller", comptroller);
            if(Storage.Get(Storage.CurrentContext, "comptroller") == null)
            {
                OnNewComptroller(null, comptroller);
            }
            else
            {
                UInt160 oldComptroller = (UInt160)Storage.Get(Storage.CurrentContext, "comptroller");
                OnNewComptroller(oldComptroller, comptroller);

            }
            

            return (uint)Error.NO_ERROR;
        }
        public static UInt160 getComptroller()
        {
            if (Storage.Get(Storage.CurrentContext, "comptroller")==null) throw new Exception("There is no comptroller");
            return (UInt160)Storage.Get(Storage.CurrentContext, "comptroller");
        }


        public static uint setInterestModel(UInt160 interestModel)
        {
            AdminSnapshot adminSnapshot = defaultAdmin.Get();
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;

            if (sender != adminSnapshot.admin)
            {
                return fail(Error.UNAUTHORIZED, FailureInfo.SET_COMPTROLLER_OWNER_CHECK);
            }


            InterestModel.Put(interestModel);

            return (uint)Error.NO_ERROR;
        }


        public static BigInteger getCashPrior()
        {
            return accountTokens.Get(Runtime.EntryScriptHash); ;
        }

        public static BigInteger doTransferIn(UInt160 from, BigInteger amount)
        {
            Transaction tx = (Transaction)Runtime.ScriptContainer;
            UInt160 sender = tx.Sender;
            if  (from  !=  sender) throw new Exception("No permission to operate");

            return amount;
        }

        public static BigInteger doTransferOut(UInt160 to,BigInteger amount)
        {
            return amount;
        }



        public static void Destroy()
        {
            //if (!IsOwner()) throw new Exception("No authorization.");
            ContractManagement.Destroy();
        }
    }
}
