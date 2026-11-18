using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DemonIdol.Network
{
    [Serializable]
    public struct RollbackProbeReport
    {
        public float meanPingMs;
        public float jitterMs;
        public float minPingMs;
        public float maxPingMs;
        public float packetLossPercent;
        public int successfulProbes;
        public int totalProbes;
        public bool isRollbackSafe;
        public string primaryEndpoint;
        public DateTime timestamp;

        public override string ToString()
        {
            return $"[RollbackReport] Ping: {meanPingMs:F1}ms (Min: {minPingMs:F1}ms, Max: {maxPingMs:F1}ms) | Jitter: {jitterMs:F1}ms | Loss: {packetLossPercent:F0}% | Safe: {isRollbackSafe}";
        }
    }

    /// <summary>
    /// Empirical Network Quality Probe for Rollback Netcode (Photon Quantum 3.0).
    /// Performs rapid sequential latency probes to compute mean Round Trip Time (RTT),
    /// jitter variance (RFC 3550 standard deviation of interarrival delay), and packet loss.
    /// </summary>
    public static class RollbackQualityProbe
    {
        public const float COMPETITIVE_MAX_PING_MS = 65.0f;
        public const float COMPETITIVE_MAX_JITTER_MS = 12.0f;
        public const int DEFAULT_PROBE_COUNT = 5;

        // Ultra-low latency edge endpoints with HTTP HEAD support
        private static readonly string[] ProbeEndpoints = new string[]
        {
            "https://www.google.com/generate_204",
            "https://1.1.1.1/cdn-cgi/trace",
            "https://firebase.google.com"
        };

        /// <summary>
        /// Runs a multi-sample probe and returns a comprehensive RollbackProbeReport.
        /// </summary>
        public static async Task<RollbackProbeReport> RunProbeAsync(int probeCount = DEFAULT_PROBE_COUNT, float maxPingThreshold = COMPETITIVE_MAX_PING_MS, float maxJitterThreshold = COMPETITIVE_MAX_JITTER_MS)
        {
            List<float> latencies = new List<float>(probeCount);
            int failedProbes = 0;
            string targetEndpoint = ProbeEndpoints[0];

            for (int i = 0; i < probeCount; i++)
            {
                float latency = await MeasureSingleProbeAsync(targetEndpoint);
                if (latency >= 0f)
                {
                    latencies.Add(latency);
                }
                else
                {
                    failedProbes++;
                }

                // Micro-interval between samples to capture realistic packet jitter
                if (i < probeCount - 1)
                {
                    await Task.Delay(40);
                }
            }

            RollbackProbeReport report = new RollbackProbeReport
            {
                totalProbes = probeCount,
                successfulProbes = latencies.Count,
                primaryEndpoint = targetEndpoint,
                timestamp = DateTime.UtcNow
            };

            if (latencies.Count == 0)
            {
                report.meanPingMs = 999f;
                report.jitterMs = 999f;
                report.minPingMs = 999f;
                report.maxPingMs = 999f;
                report.packetLossPercent = 100f;
                report.isRollbackSafe = false;
                return report;
            }

            report.packetLossPercent = ((float)failedProbes / probeCount) * 100f;

            // Compute Min, Max, Mean Ping
            float sum = 0f;
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = 0; i < latencies.Count; i++)
            {
                float val = latencies[i];
                sum += val;
                if (val < min) min = val;
                if (val > max) max = val;
            }

            report.meanPingMs = sum / latencies.Count;
            report.minPingMs = min;
            report.maxPingMs = max;

            // Compute RFC 3550 Mean Jitter (mean of absolute differences between consecutive latencies)
            if (latencies.Count > 1)
            {
                float jitterSum = 0f;
                for (int i = 0; i < latencies.Count - 1; i++)
                {
                    jitterSum += Mathf.Abs(latencies[i + 1] - latencies[i]);
                }
                report.jitterMs = jitterSum / (latencies.Count - 1);
            }
            else
            {
                report.jitterMs = 0f;
            }

            // A match is rollback safe if latency is low, jitter is tight, and 0% loss detected
            report.isRollbackSafe = (report.meanPingMs <= maxPingThreshold) &&
                                   (report.jitterMs <= maxJitterThreshold) &&
                                   (report.packetLossPercent <= 0.01f);

            return report;
        }

        private static async Task<float> MeasureSingleProbeAsync(string url)
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                using UnityWebRequest req = UnityWebRequest.Head(url);
                req.timeout = 2; // 2 seconds hard timeout
                var operation = req.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                sw.Stop();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    return (float)sw.Elapsed.TotalMilliseconds;
                }
                return -1f;
            }
            catch
            {
                sw.Stop();
                return -1f;
            }
        }
    }
}
