﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CashFlower.Contracts;
using CashFlower.Framework;

namespace CashFlower.BankTransferReader.AbnAmro.Tab
{
    public class AbnAmroTabReader: IBankTransferReader
    {
        private readonly string _fullFilename ;
        private readonly char[] _tabDelimiter = new char[] { '\t' };
        private const string Currency = "EUR";

        public AbnAmroTabReader(string fullFilename)
        {
            _fullFilename = fullFilename;
        }

        public List<BankTransferLine> GetBankTransfers()
        {
            if (!File.Exists(_fullFilename))
                throw new FileNotFoundException(_fullFilename);

            var result = new List<BankTransferLine>();
            using (var file = new StreamReader(_fullFilename))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    result.Add(_parseLine(line));
                    throw new NotImplementedException();
                }
            }
            return result;
        }

        private BankTransferLine _parseLine(string line)
        {
            var parts = line.Split(_tabDelimiter, StringSplitOptions.None);
            _validateNumberOfParts(line, parts);
            _validateCurrency(parts[1]);

            return 
                new BankTransferLine {
                    AccountNumber = parts[0],
                    TransactionDate = _retrieveTransactionDate(parts[2]),
                    InitialBalance = _retrieveInitialBalance(parts[3]),
                    FinalBalance = _retrieveFinalBalance(parts[4]),
                    InterestDate=_retrieveInterestDate(parts[5]),
                    Amount = _retrieveAmount(parts[6])
                };
        }

        private decimal _retrieveAmount(string amount)
        {
            try
            {
                return _parseAbnAmroDecimalString(amount);
            }
            catch (Exception ex)
            {
                throw new CashFlowerException("CFE_ABN_007", "Failed to parse the amount ({0}) " +
                    "because of the following exception: {1}",
                    amount,
                    ex.Message);
            }
        }

        private decimal _retrieveFinalBalance(string finalBalance)
        {
            try
            {
                return _parseAbnAmroDecimalString(finalBalance);
            }
            catch (Exception ex)
            {
                throw new CashFlowerException("CFE_ABN_005", "Failed to parse the final balance ({0}) " +
                    "because of the following exception: {1}",
                    finalBalance,
                    ex.Message);
            }
        }

        private static void _validateNumberOfParts(string line, string[] parts)
        {
            if (parts.Count() != 8)
            {
                throw new CashFlowerException(
                    "CFE_ABN_001", "Wrong number ({0}) of TABs in line : {1}"
                    , _getNumberOfTabs(parts)
                    , line);
            }
        }

        private decimal _retrieveInitialBalance(string initialBalance)
        {
            try
            {
                return _parseAbnAmroDecimalString(initialBalance);
            }
            catch (Exception ex)
            {
                throw new CashFlowerException("CFE_ABN_004","Failed to parse the initial balance ({0}) " +
                    "because of the following exception: {1}",
                    initialBalance,
                    ex.Message);
            }
        }

        private decimal _parseAbnAmroDecimalString(string initialBalance)
        {
            if (initialBalance.Contains(".")) throw new ArgumentException("ABN AMRO decimal numbers never contain dots.");

            if (initialBalance.IndexOf(",", StringComparison.InvariantCulture)==-1) throw new ArgumentException("ABN AMRO decimals always contain one comma.");

            return
                decimal.Parse(
                    initialBalance.Replace(",", CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture);
        }

        private static void _validateCurrency(string currency)
        {
            if (!currency.Equals(Currency))
                throw new CashFlowerException("CFE_ABN_002", "No Valid currrency ({0}) given.", currency);
        }

        private static DateTime _retrieveTransactionDate(string transActionDate)
        {
            DateTime myDate;
            if (_tryParseAbnAmroDateString(transActionDate, out myDate))
                return myDate;
            throw new CashFlowerException("CFE_ABN_003", "No Valid transactiondate ({0}) given.", transActionDate);
        }

        private static bool _tryParseAbnAmroDateString(string transActionDate, out DateTime myDate)
        {
            return DateTime.TryParseExact(transActionDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out myDate);
        }

        private DateTime _retrieveInterestDate(string interestDate)
        {
            DateTime myDate;
            if (_tryParseAbnAmroDateString(interestDate, out myDate))
                return myDate;
            throw new CashFlowerException("CFE_ABN_006", "No Valid transactiondate ({0}) given.", interestDate);
        }

        private static int _getNumberOfTabs(IEnumerable<string> parts)
        {
            return (parts.Count() - 1);
        }
    }
}
