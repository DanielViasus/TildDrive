using UnityEngine;
using TiltDrive.TransmissionSystem;

namespace TiltDrive.Simulation
{
    public static class DriveMisuseDiagnostics
    {
        public struct MisuseReport
        {
            public bool hasFault;
            public string code;
            public string message;
            public float severity;
            public float requiredEngineRPM;
            public float maxEngineRPM;
            public float currentEngineRPM;
            public float throttleInput;
            public int currentGear;
            public float engineDamagePercent;
            public float transmissionDamagePercent;
        }

        public struct LaunchReport
        {
            public bool hasWarning;
            public bool hasMisuse;
            public bool stallRisk;
            public bool forceStall;
            public string code;
            public string message;
            public float severity;
            public float rpmPenalty;
        }

        public static LaunchReport AnalyzeLaunchOperation(
            int currentGear,
            float vehicleSpeedMS,
            float throttleInput,
            float brakeInput,
            float clutchInput,
            float clutchEngagement,
            float currentRPM,
            float idleRPM,
            float stallRPM,
            DriveLaunchDiagnosticsConfig launchConfig)
        {
            LaunchReport report = new LaunchReport
            {
                code = string.Empty,
                message = string.Empty
            };

            if (launchConfig == null)
            {
                launchConfig = new DriveLaunchDiagnosticsConfig();
            }

            launchConfig.ClampValues();

            bool inLaunchRange = Mathf.Abs(vehicleSpeedMS) <= launchConfig.launchSpeedThresholdMS;
            bool inDriveGear = currentGear != 0;
            bool inBiteZone = clutchEngagement >= launchConfig.biteEngagementMin;
            bool suitableIdleLaunchGear = currentGear < 0 || currentGear <= launchConfig.maxRecommendedLaunchGear;
            bool attemptingIdleOnlyLaunch = launchConfig.allowIdleOnlyLaunch &&
                suitableIdleLaunchGear &&
                throttleInput <= launchConfig.idleOnlyThrottleThreshold &&
                currentRPM >= stallRPM + launchConfig.idleOnlyRPMReserve;

            if (!inLaunchRange || !inDriveGear || !inBiteZone)
            {
                return report;
            }

            if (currentGear > launchConfig.maxRecommendedLaunchGear)
            {
                float gearSeverity = Mathf.Clamp01(
                    (currentGear - launchConfig.maxRecommendedLaunchGear) /
                    Mathf.Max(1f, 6f - launchConfig.maxRecommendedLaunchGear));
                SetLaunchIssue(
                    ref report,
                    "START_IN_HIGH_GEAR",
                    $"Launching in gear {currentGear}; use gear {launchConfig.maxRecommendedLaunchGear}.",
                    Mathf.Max(0.45f, gearSeverity),
                    true);
            }

            if (brakeInput >= launchConfig.brakeConflictThreshold && throttleInput > 0.05f)
            {
                SetLaunchIssue(
                    ref report,
                    "BRAKE_THROTTLE_CONFLICT",
                    "Throttle applied while brake is still pressed during launch.",
                    0.45f + brakeInput * 0.35f,
                    true);
            }

            if (clutchEngagement >= launchConfig.biteEngagementMin &&
                clutchEngagement <= launchConfig.biteEngagementMax &&
                throttleInput < launchConfig.minThrottleAtBite &&
                !attemptingIdleOnlyLaunch)
            {
                float throttleGap = Mathf.InverseLerp(launchConfig.minThrottleAtBite, 0f, throttleInput);
                SetLaunchIssue(
                    ref report,
                    "LOW_THROTTLE_AT_BITE",
                    "Too little throttle at clutch bite point; engine may stall.",
                    Mathf.Lerp(0.35f, 0.75f, throttleGap),
                    false);
            }

            if (clutchEngagement >= launchConfig.clutchDumpEngagement &&
                throttleInput < launchConfig.minThrottleForClutchDump)
            {
                float throttleGap = Mathf.InverseLerp(launchConfig.minThrottleForClutchDump, 0f, throttleInput);
                float rpmReserveFactor = Mathf.InverseLerp(stallRPM, Mathf.Max(stallRPM + 1f, idleRPM), currentRPM);
                SetLaunchIssue(
                    ref report,
                    "CLUTCH_DUMP_LOW_THROTTLE",
                    "Clutch released too quickly with insufficient throttle.",
                    Mathf.Clamp01(Mathf.Lerp(0.65f, 1f, throttleGap) + (1f - rpmReserveFactor) * 0.2f),
                    true);
            }

            report.severity = Mathf.Clamp01(report.severity);
            report.stallRisk = report.severity >= launchConfig.stallRiskThreshold;
            report.rpmPenalty = report.stallRisk
                ? launchConfig.maxLaunchRPMPenalty * report.severity
                : launchConfig.maxLaunchRPMPenalty * report.severity * 0.5f;
            report.forceStall = report.stallRisk &&
                currentRPM <= stallRPM + launchConfig.stallRPMMargin &&
                clutchEngagement >= launchConfig.clutchDumpEngagement &&
                throttleInput < launchConfig.minThrottleAtBite;

            return report;
        }

        public static MisuseReport AnalyzeGearChange(
            int previousGear,
            int targetGear,
            float vehicleSpeedMS,
            float wheelCircumferenceMeters,
            float engineMaxRPM,
            TransmissionConfig transmissionConfig,
            DriveDamagePenaltyConfig damagePenaltyConfig)
        {
            MisuseReport report = new MisuseReport
            {
                code = string.Empty,
                message = string.Empty,
                maxEngineRPM = Mathf.Max(0f, engineMaxRPM)
            };

            if (damagePenaltyConfig == null)
            {
                damagePenaltyConfig = new DriveDamagePenaltyConfig();
            }

            damagePenaltyConfig.ClampValues();

            if (transmissionConfig == null ||
                wheelCircumferenceMeters <= 0f)
            {
                return report;
            }

            if (targetGear < 0 && previousGear > 0 && Mathf.Abs(vehicleSpeedMS) >= damagePenaltyConfig.reverseMisuseMinSpeedMS)
            {
                report.hasFault = true;
                report.code = "REVERSE_WHILE_MOVING";
                report.severity = 1f;
                report.requiredEngineRPM = 0f;
                report.engineDamagePercent = Mathf.Min(
                    damagePenaltyConfig.reverseWhileMovingEngineDamagePercent,
                    damagePenaltyConfig.maxSingleEventEngineDamagePercent);
                report.transmissionDamagePercent = Mathf.Min(
                    damagePenaltyConfig.reverseWhileMovingTransmissionDamagePercent,
                    damagePenaltyConfig.maxSingleEventTransmissionDamagePercent);
                report.message =
                    $"Reverse requested while moving from gear {previousGear} at {Mathf.Abs(vehicleSpeedMS) * 3.6f:F1} km/h.";

                return report;
            }

            if (previousGear <= 0 ||
                targetGear <= 0 ||
                targetGear >= previousGear ||
                engineMaxRPM <= 0f)
            {
                return report;
            }

            float targetGearRatio = transmissionConfig.GetGearRatio(targetGear);
            float targetTotalDriveRatio = targetGearRatio * transmissionConfig.finalDriveRatio;
            if (targetTotalDriveRatio <= 0f)
            {
                return report;
            }

            float wheelRPM = Mathf.Abs(vehicleSpeedMS) / wheelCircumferenceMeters * 60f;
            float requiredEngineRPM = wheelRPM * targetTotalDriveRatio;
            report.requiredEngineRPM = requiredEngineRPM;

            if (requiredEngineRPM <= engineMaxRPM)
            {
                return report;
            }

            float overRevRPM = requiredEngineRPM - engineMaxRPM;
            float severity = Mathf.Clamp01(
                overRevRPM / Mathf.Max(1f, engineMaxRPM * damagePenaltyConfig.overRevSeverityRPMWindowFactor));
            int skippedGearCount = Mathf.Max(0, previousGear - targetGear - 1);
            float skippedGearMultiplier = 1f + skippedGearCount * damagePenaltyConfig.skippedGearDamageMultiplier;
            float severityMultiplier = Mathf.Lerp(1f, damagePenaltyConfig.fullSeverityDamageMultiplier, severity);
            float damageMultiplier = skippedGearMultiplier * severityMultiplier;

            report.hasFault = true;
            report.code = "BAD_DOWNSHIFT_OVERREV";
            report.severity = severity;
            report.engineDamagePercent = Mathf.Min(
                Mathf.Lerp(
                    damagePenaltyConfig.downshiftEngineDamageMinPercent,
                    damagePenaltyConfig.downshiftEngineDamageMaxPercent,
                    severity) * damageMultiplier,
                damagePenaltyConfig.maxSingleEventEngineDamagePercent);
            report.transmissionDamagePercent = Mathf.Min(
                Mathf.Lerp(
                    damagePenaltyConfig.downshiftTransmissionDamageMinPercent,
                    damagePenaltyConfig.downshiftTransmissionDamageMaxPercent,
                    severity) * damageMultiplier,
                damagePenaltyConfig.maxSingleEventTransmissionDamagePercent);
            report.message =
                $"Bad downshift {previousGear}->{targetGear}: required RPM {requiredEngineRPM:F0} exceeds max {engineMaxRPM:F0}. SkippedGears={skippedGearCount}.";

            return report;
        }

        private static void SetLaunchIssue(
            ref LaunchReport report,
            string code,
            string message,
            float severity,
            bool misuse)
        {
            float clampedSeverity = Mathf.Clamp01(severity);
            if (clampedSeverity < report.severity)
            {
                return;
            }

            report.hasWarning = true;
            report.hasMisuse = misuse;
            report.code = code;
            report.message = message;
            report.severity = clampedSeverity;
        }

        public static MisuseReport AnalyzeHighGearEngineStrain(
            int currentGear,
            float vehicleSpeedMS,
            float throttleInput,
            float clutchEngagement,
            float currentRPM,
            DriveDamagePenaltyConfig damagePenaltyConfig)
        {
            MisuseReport report = new MisuseReport
            {
                code = string.Empty,
                message = string.Empty,
                currentGear = currentGear,
                currentEngineRPM = Mathf.Max(0f, currentRPM),
                throttleInput = Mathf.Clamp01(throttleInput)
            };

            if (damagePenaltyConfig == null)
            {
                damagePenaltyConfig = new DriveDamagePenaltyConfig();
            }

            damagePenaltyConfig.ClampValues();

            bool movingHighGearStrain =
                damagePenaltyConfig.highGearEngineStrainEnabled &&
                currentGear >= damagePenaltyConfig.highGearStrainMinGear &&
                Mathf.Abs(vehicleSpeedMS) >= damagePenaltyConfig.highGearStrainMinSpeedMS &&
                throttleInput >= damagePenaltyConfig.highGearStrainThrottleThreshold &&
                clutchEngagement >= damagePenaltyConfig.highGearStrainMinClutchEngagement &&
                currentRPM > 0f &&
                currentRPM <= damagePenaltyConfig.highGearStrainRPMThreshold;

            bool launchHighGearStrain =
                damagePenaltyConfig.highGearLaunchStrainEnabled &&
                currentGear >= damagePenaltyConfig.highGearLaunchStrainMinGear &&
                Mathf.Abs(vehicleSpeedMS) <= damagePenaltyConfig.highGearLaunchStrainMaxSpeedMS &&
                throttleInput >= damagePenaltyConfig.highGearLaunchStrainThrottleThreshold &&
                clutchEngagement >= damagePenaltyConfig.highGearLaunchStrainMinClutchEngagement &&
                currentRPM > 0f;

            if (!movingHighGearStrain && !launchHighGearStrain)
            {
                return report;
            }

            if (launchHighGearStrain)
            {
                float launchThrottleSeverity = Mathf.InverseLerp(
                    damagePenaltyConfig.highGearLaunchStrainThrottleThreshold,
                    1f,
                    throttleInput);
                float launchGearSeverity = Mathf.InverseLerp(
                    damagePenaltyConfig.highGearLaunchStrainMinGear,
                    Mathf.Max(damagePenaltyConfig.highGearLaunchStrainMinGear + 1, 6),
                    currentGear);
                float launchClutchSeverity = Mathf.InverseLerp(
                    damagePenaltyConfig.highGearLaunchStrainMinClutchEngagement,
                    1f,
                    clutchEngagement);

                report.hasFault = true;
                report.code = "HIGH_GEAR_ENGINE_STRAIN_LAUNCH";
                report.severity = Mathf.Clamp01(Mathf.Max(
                    0.60f,
                    launchThrottleSeverity,
                    launchGearSeverity,
                    launchClutchSeverity));
                report.requiredEngineRPM = damagePenaltyConfig.highGearStrainRPMThreshold;
                report.message =
                    $"High gear launch attempt in gear {currentGear}; start in 1st gear to avoid engine strain.";

                return report;
            }

            float rpmSeverity = Mathf.InverseLerp(
                damagePenaltyConfig.highGearStrainRPMThreshold,
                damagePenaltyConfig.highGearStrainCriticalRPM,
                currentRPM);
            float throttleSeverity = Mathf.InverseLerp(
                damagePenaltyConfig.highGearStrainThrottleThreshold,
                1f,
                throttleInput);
            float gearSeverity = Mathf.InverseLerp(
                damagePenaltyConfig.highGearStrainMinGear,
                Mathf.Max(damagePenaltyConfig.highGearStrainMinGear + 1, 6),
                currentGear);

            report.hasFault = true;
            report.code = "HIGH_GEAR_ENGINE_STRAIN";
            report.severity = Mathf.Clamp01(Mathf.Max(rpmSeverity, throttleSeverity, gearSeverity));
            report.requiredEngineRPM = damagePenaltyConfig.highGearStrainRPMThreshold;
            report.message =
                $"Engine strained by heavy throttle in gear {currentGear} at low RPM ({currentRPM:F0}). Downshift or reduce throttle.";

            return report;
        }

        public static MisuseReport AnalyzeActiveGearConnection(
            int currentGear,
            float vehicleSpeedMS,
            float wheelCircumferenceMeters,
            float engineMaxRPM,
            TransmissionConfig transmissionConfig,
            DriveDamagePenaltyConfig damagePenaltyConfig)
        {
            MisuseReport report = new MisuseReport
            {
                code = string.Empty,
                message = string.Empty,
                currentGear = currentGear,
                maxEngineRPM = Mathf.Max(0f, engineMaxRPM)
            };

            if (damagePenaltyConfig == null)
            {
                damagePenaltyConfig = new DriveDamagePenaltyConfig();
            }

            damagePenaltyConfig.ClampValues();

            if (transmissionConfig == null || wheelCircumferenceMeters <= 0f)
            {
                return report;
            }

            if (currentGear < 0 && Mathf.Abs(vehicleSpeedMS) >= damagePenaltyConfig.reverseMisuseMinSpeedMS)
            {
                report.hasFault = true;
                report.code = "REVERSE_WHILE_MOVING";
                report.severity = 1f;
                report.engineDamagePercent = Mathf.Min(
                    damagePenaltyConfig.reverseWhileMovingEngineDamagePercent,
                    damagePenaltyConfig.maxSingleEventEngineDamagePercent);
                report.transmissionDamagePercent = Mathf.Min(
                    damagePenaltyConfig.reverseWhileMovingTransmissionDamagePercent,
                    damagePenaltyConfig.maxSingleEventTransmissionDamagePercent);
                report.message =
                    $"Reverse engaged while moving at {Mathf.Abs(vehicleSpeedMS) * 3.6f:F1} km/h.";
                return report;
            }

            if (currentGear <= 0 || engineMaxRPM <= 0f)
            {
                return report;
            }

            float targetGearRatio = transmissionConfig.GetGearRatio(currentGear);
            float targetTotalDriveRatio = targetGearRatio * transmissionConfig.finalDriveRatio;
            if (targetTotalDriveRatio <= 0f)
            {
                return report;
            }

            float wheelRPM = Mathf.Abs(vehicleSpeedMS) / wheelCircumferenceMeters * 60f;
            float requiredEngineRPM = wheelRPM * targetTotalDriveRatio;
            report.requiredEngineRPM = requiredEngineRPM;

            if (requiredEngineRPM <= engineMaxRPM)
            {
                return report;
            }

            float overRevRPM = requiredEngineRPM - engineMaxRPM;
            float severity = Mathf.Clamp01(
                overRevRPM / Mathf.Max(1f, engineMaxRPM * damagePenaltyConfig.overRevSeverityRPMWindowFactor));
            float severityMultiplier = Mathf.Lerp(1f, damagePenaltyConfig.fullSeverityDamageMultiplier, severity);

            report.hasFault = true;
            report.code = "BAD_DOWNSHIFT_OVERREV";
            report.severity = severity;
            report.engineDamagePercent = Mathf.Min(
                Mathf.Lerp(
                    damagePenaltyConfig.downshiftEngineDamageMinPercent,
                    damagePenaltyConfig.downshiftEngineDamageMaxPercent,
                    severity) * severityMultiplier,
                damagePenaltyConfig.maxSingleEventEngineDamagePercent);
            report.transmissionDamagePercent = Mathf.Min(
                Mathf.Lerp(
                    damagePenaltyConfig.downshiftTransmissionDamageMinPercent,
                    damagePenaltyConfig.downshiftTransmissionDamageMaxPercent,
                    severity) * severityMultiplier,
                damagePenaltyConfig.maxSingleEventTransmissionDamagePercent);
            report.message =
                $"Unsafe clutch release in gear {currentGear}: required RPM {requiredEngineRPM:F0} exceeds max {engineMaxRPM:F0}.";

            return report;
        }

        public static MisuseReport AnalyzeDownshift(
            int previousGear,
            int targetGear,
            float vehicleSpeedMS,
            float wheelCircumferenceMeters,
            float engineMaxRPM,
            TransmissionConfig transmissionConfig)
        {
            return AnalyzeGearChange(
                previousGear,
                targetGear,
                vehicleSpeedMS,
                wheelCircumferenceMeters,
                engineMaxRPM,
                transmissionConfig,
                new DriveDamagePenaltyConfig());
        }
    }
}
