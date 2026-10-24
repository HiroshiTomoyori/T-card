using UnityEngine;

public struct CoinTossPose
{
    public Vector2 center;
    public float diameter, cosine, sine, rollDegrees, arc, time;
}

public static class CoinTossMotion
{
    public static CoinTossPose Evaluate(CoinTossSettings s, float progress, bool butterfly)
    {
        float t = Mathf.Clamp01(progress);
        float p = Mathf.Clamp01(t / Mathf.Max(0.001f, s.flightPortion));
        float arc = 4f * p * (1f - p);
        float spin = Mathf.Clamp01(t / Mathf.Max(0.001f, s.spinPortion));
        float brake = Mathf.Clamp(s.brakingStart, 0.1f, 0.99f);
        float denominator = brake + (1f - brake) * 0.5f;
        float u = Mathf.Clamp01((spin - brake) / (1f - brake));
        float phase = spin < brake ? spin / denominator
            : (brake + (1f - brake) * (u - u * u * 0.5f)) / denominator;
        float target = (Mathf.Max(1, s.fullRotations) * 360f + (butterfly ? 0f : 180f)) * Mathf.Deg2Rad;
        float angle = Mathf.Lerp(s.initialFlipDegrees * Mathf.Deg2Rad, target, phase);
        float bounce = 0f;
        if (t > s.flightPortion)
        {
            float b = Mathf.Clamp01((t - s.flightPortion) / Mathf.Max(0.001f, 1f - s.flightPortion));
            bounce = s.bounceHeight * Mathf.Sin(Mathf.PI * b) * (1f - b);
            if (t > s.spinPortion)
            {
                float w = Mathf.Clamp01((t - s.spinPortion) / Mathf.Max(0.001f, 1f - s.spinPortion));
                angle += s.settlingWobbleDegrees * Mathf.Deg2Rad * Mathf.Sin(2f * Mathf.PI * w) * (1f - w) * (1f - w);
            }
        }
        float diameter = Mathf.Lerp(s.startDiameter, s.endDiameter, p * p * (3f - 2f * p))
            / Mathf.Max(0.1f, 1f - Mathf.Clamp(s.perspectiveStrength, 0f, 0.65f) * arc);
        return new CoinTossPose
        {
            center = new Vector2(216f + s.lateralDrift * Mathf.Sin(Mathf.PI * p),
                924f * s.restingHeightFromTop - s.jumpHeight * arc - bounce) + s.positionOffset,
            diameter = Mathf.Max(1f, diameter * Mathf.Max(0.1f, s.overallScale)),
            cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle),
            rollDegrees = s.initialRollDegrees * (1f - p) + s.rollSwayDegrees * Mathf.Sin(Mathf.PI * p),
            arc = arc, time = t * s.duration
        };
    }
}
