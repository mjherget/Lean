/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic template framework algorithm uses framework components to define the algorithm.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="using quantconnect" />
    /// <meta name="tag" content="trading and orders" />
    public class BasicTemplateFrameworkAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const string TechnicalIndicatorsChartName = "Technical Indicators";
        private BasicTemplateFrameworkSettings _backtestSettings;
        private Symbol _symbol;
        private SimpleMovingAverage _sma;
        private ExponentialMovingAverage _emaFast;
        private ExponentialMovingAverage _emaSlow;
        private RelativeStrengthIndex _rsi;
        private MovingAverageConvergenceDivergence _macd;
        private BollingerBands _bollingerBands;

        /// <summary>
        /// Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.
        /// </summary>
        public override void Initialize()
        {
            var backtestSettings = BasicTemplateFrameworkSettings.FromParameters(GetParameters(), Debug);
            _backtestSettings = backtestSettings;

            // Set requested data resolution
            UniverseSettings.Resolution = backtestSettings.Resolution;

            SetStartDate(backtestSettings.StartDate);  //Set Start Date
            SetEndDate(backtestSettings.EndDate);      //Set End Date
            SetCash(backtestSettings.Cash);            //Set Strategy Cash

            // Find more symbols here: http://quantconnect.com/data
            // Forex, CFD, Equities Resolutions: Tick, Second, Minute, Hour, Daily.
            // Futures Resolution: Tick, Second, Minute
            // Options Resolution: Minute Only.
            _symbol = QuantConnect.Symbol.Create(backtestSettings.Ticker, SecurityType.Equity, Market.USA);
            AddEquity(backtestSettings.Ticker, backtestSettings.Resolution);
            ConfigureTechnicalIndicators(backtestSettings);

            // set algorithm framework models
            SetUniverseSelection(new ManualUniverseSelectionModel(_symbol));
            SetAlpha(new ConstantAlphaModel(InsightType.Price, InsightDirection.Up, TimeSpan.FromMinutes(20), 0.025, null));

            // We can define who often the EWPCM will rebalance if no new insight is submitted using:
            // Resolution Enum:
            SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(Resolution.Daily));
            // TimeSpan
            // SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(TimeSpan.FromDays(2)));
            // A Func<DateTime, DateTime>. In this case, we can use the pre-defined func at Expiry helper class
            // SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModel(Expiry.EndOfWeek));

            SetExecution(new ImmediateExecutionModel());
            SetRiskManagement(new MaximumDrawdownPercentPerSecurity(0.01m));
        }

        public override void OnData(Slice slice)
        {
            PlotTechnicalIndicators();
        }

        private void ConfigureTechnicalIndicators(BasicTemplateFrameworkSettings settings)
        {
            if (settings.SmaEnabled)
            {
                _sma = SMA(_symbol, settings.SmaPeriod, settings.Resolution);
            }

            if (settings.EmaEnabled)
            {
                _emaFast = EMA(_symbol, settings.EmaFastPeriod, settings.Resolution);
                _emaSlow = EMA(_symbol, settings.EmaSlowPeriod, settings.Resolution);
            }

            if (settings.RsiEnabled)
            {
                _rsi = RSI(_symbol, settings.RsiPeriod, MovingAverageType.Wilders, settings.Resolution);
            }

            if (settings.MacdEnabled)
            {
                _macd = MACD(_symbol, settings.MacdFastPeriod, settings.MacdSlowPeriod, settings.MacdSignalPeriod, MovingAverageType.Exponential, settings.Resolution);
            }

            if (settings.BollingerBandsEnabled)
            {
                _bollingerBands = BB(_symbol, settings.BollingerBandsPeriod, settings.BollingerBandsStandardDeviations, MovingAverageType.Simple, settings.Resolution);
            }
        }

        private void PlotTechnicalIndicators()
        {
            if (_backtestSettings.SmaEnabled && _sma?.IsReady == true)
            {
                Plot(TechnicalIndicatorsChartName, $"SMA {_backtestSettings.SmaPeriod}", _sma.Current.Value);
            }

            if (_backtestSettings.EmaEnabled)
            {
                if (_emaFast?.IsReady == true)
                {
                    Plot(TechnicalIndicatorsChartName, $"EMA {_backtestSettings.EmaFastPeriod}", _emaFast.Current.Value);
                }

                if (_emaSlow?.IsReady == true)
                {
                    Plot(TechnicalIndicatorsChartName, $"EMA {_backtestSettings.EmaSlowPeriod}", _emaSlow.Current.Value);
                }
            }

            if (_backtestSettings.RsiEnabled && _rsi?.IsReady == true)
            {
                Plot(TechnicalIndicatorsChartName, $"RSI {_backtestSettings.RsiPeriod}", _rsi.Current.Value);
            }

            if (_backtestSettings.MacdEnabled && _macd?.IsReady == true)
            {
                Plot(TechnicalIndicatorsChartName, "MACD", _macd.Current.Value);
                Plot(TechnicalIndicatorsChartName, "MACD Signal", _macd.Signal.Current.Value);
            }

            if (_backtestSettings.BollingerBandsEnabled && _bollingerBands?.IsReady == true)
            {
                Plot(TechnicalIndicatorsChartName, "BB Upper", _bollingerBands.UpperBand.Current.Value);
                Plot(TechnicalIndicatorsChartName, "BB Middle", _bollingerBands.MiddleBand.Current.Value);
                Plot(TechnicalIndicatorsChartName, "BB Lower", _bollingerBands.LowerBand.Current.Value);
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status.IsFill())
            {
                Debug($"Purchased Stock: {orderEvent.Symbol}");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual List<Language> Languages { get; } = new() { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public long DataPoints => 3943;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// Final status of the algorithm
        /// </summary>
        public AlgorithmStatus AlgorithmStatus => AlgorithmStatus.Completed;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Orders", "3"},
            {"Average Win", "0%"},
            {"Average Loss", "-1.01%"},
            {"Compounding Annual Return", "261.134%"},
            {"Drawdown", "2.200%"},
            {"Expectancy", "-1"},
            {"Start Equity", "100000"},
            {"End Equity", "101655.30"},
            {"Net Profit", "1.655%"},
            {"Sharpe Ratio", "8.472"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "66.840%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "-0.091"},
            {"Beta", "1.006"},
            {"Annual Standard Deviation", "0.224"},
            {"Annual Variance", "0.05"},
            {"Information Ratio", "-33.445"},
            {"Tracking Error", "0.002"},
            {"Treynor Ratio", "1.885"},
            {"Total Fees", "$10.32"},
            {"Estimated Strategy Capacity", "$27000000.00"},
            {"Lowest Capacity Asset", "SPY R735QTJ8XC9X"},
            {"Portfolio Turnover", "59.86%"},
            {"Drawdown Recovery", "3"},
            {"OrderListHash", "f209ed42701b0419858e0100595b40c0"}
        };
    }
}
