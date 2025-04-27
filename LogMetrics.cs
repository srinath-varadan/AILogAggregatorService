using Prometheus;

namespace LogAggregatorService.LogMetrics
{
    public static class LogMetrics
    {
        // General Metrics
        public static readonly Counter TotalLogsProcessed = Metrics.CreateCounter(
            "logs_total_processed",
            "Total logs processed across all sources"
        );

        // Info Level Metrics
        public static readonly Counter InfoLogsProcessed = Metrics.CreateCounter(
            "logs_info_total",
            "Total info-level logs processed"
        );

        // Warning Level Metrics
        public static readonly Counter WarningLogsProcessed = Metrics.CreateCounter(
            "logs_warning_total",
            "Total warning-level logs processed"
        );

        // Error Level Metrics
        public static readonly Counter ErrorLogsProcessed = Metrics.CreateCounter(
            "logs_error_total",
            "Total error-level logs processed"
        );

        // Anomaly Detection Metrics
        public static readonly Gauge AnomalyDetected = Metrics.CreateGauge(
            "logs_anomaly_detected",
            "1 if anomaly detected, 0 if normal"
        );

        public static readonly Counter TotalAnomaliesDetected = Metrics.CreateCounter(
            "logs_total_anomalies_detected",
            "Total number of anomalies detected by AI analysis"
        );

        /// <summary>
        /// Call this after AI log summarization.
        /// </summary>
        public static void ProcessAIInsights(string aiOutput)
        {
            if (string.IsNullOrEmpty(aiOutput))
                return;

            if (aiOutput.Contains("info", StringComparison.OrdinalIgnoreCase))
                InfoLogsProcessed.Inc();

            if (aiOutput.Contains("warning", StringComparison.OrdinalIgnoreCase))
                WarningLogsProcessed.Inc();

            if (aiOutput.Contains("error", StringComparison.OrdinalIgnoreCase))
                ErrorLogsProcessed.Inc();

            if (aiOutput.Contains("anomaly", StringComparison.OrdinalIgnoreCase))
            {
                AnomalyDetected.Set(1);
                TotalAnomaliesDetected.Inc();
            }
            else
            {
                AnomalyDetected.Set(0);
            }

            TotalLogsProcessed.Inc();
        }
    }
}