using System.Collections.Generic;

public sealed class ExperienceProgressPresentation
{
    public enum Phase
    {
        Idle,
        Filling,
        HoldingFull
    }

    private const float DefaultFullBarDuration = 0.35f;
    private const float DefaultMinimumSegmentDuration = 0.08f;
    private const float DefaultFullHoldDuration = 0.10f;
    private const float TimingEpsilon = 0.000001f;

    private readonly Queue<int> pendingLevels = new Queue<int>();

    private int requiredExperience = 1;
    private int displayedExperience;
    private int pendingExperience;
    private int projectedExperience;
    private int projectedLevel = 1;
    private int segmentStart;
    private int segmentTarget;
    private float segmentElapsed;
    private float segmentDuration;
    private float holdElapsed;
    private float fullBarDuration = DefaultFullBarDuration;
    private float minimumSegmentDuration =
        DefaultMinimumSegmentDuration;
    private float fullHoldDuration = DefaultFullHoldDuration;
    private bool segmentReachesFull;

    public Phase CurrentPhase { get; private set; } = Phase.Idle;
    public float FillAmount { get; private set; }
    public int DisplayedLevel { get; private set; } = 1;
    public int DisplayedExperience => displayedExperience;
    public int RequiredExperience => requiredExperience;
    public bool IsAnimating => CurrentPhase != Phase.Idle;

    public ExperienceProgressPresentation()
    {
    }

    public ExperienceProgressPresentation(
        float totalFillDuration,
        float minSegmentDuration,
        float fullHoldDuration
    )
    {
        Configure(
            totalFillDuration,
            minSegmentDuration,
            fullHoldDuration
        );
    }

    public void Configure(
        float totalFillDuration,
        float minSegmentDuration,
        float fullHoldDuration
    )
    {
        fullBarDuration = PositiveOrDefault(
            totalFillDuration,
            DefaultFullBarDuration
        );
        minimumSegmentDuration = PositiveOrDefault(
            minSegmentDuration,
            DefaultMinimumSegmentDuration
        );
        this.fullHoldDuration = PositiveOrDefault(
            fullHoldDuration,
            DefaultFullHoldDuration
        );
    }

    public void Reset(
        int currentExperience,
        int requiredExperience,
        int level
    )
    {
        this.requiredExperience = Max(1, requiredExperience);
        displayedExperience = Clamp(
            currentExperience,
            0,
            this.requiredExperience - 1
        );
        DisplayedLevel = Max(1, level);
        FillAmount = Ratio(displayedExperience);
        pendingExperience = 0;
        pendingLevels.Clear();
        projectedExperience = displayedExperience;
        projectedLevel = DisplayedLevel;
        CurrentPhase = Phase.Idle;
        segmentElapsed = 0f;
        holdElapsed = 0f;
    }

    public void EnqueueExperience(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        pendingExperience += amount;
        projectedExperience += amount;

        while (projectedExperience >= requiredExperience)
        {
            projectedExperience -= requiredExperience;
        }

        if (CurrentPhase == Phase.Idle)
        {
            StartNextSegment();
        }
    }

    public void EnqueueLevel(int level)
    {
        level = Max(1, level);

        if (level != DisplayedLevel + pendingLevels.Count + 1)
        {
            CancelToAuthoritativeState(
                projectedExperience,
                requiredExperience,
                level
            );
            return;
        }

        pendingLevels.Enqueue(level);
        projectedLevel = level;
    }

    public void Advance(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        float remaining = deltaTime;

        while (remaining > TimingEpsilon &&
               CurrentPhase != Phase.Idle)
        {
            if (CurrentPhase == Phase.Filling)
            {
                AdvanceFill(ref remaining);
                continue;
            }

            AdvanceHold(ref remaining);
        }
    }

    private void AdvanceFill(ref float remaining)
    {
        float durationLeft = segmentDuration - segmentElapsed;

        if (remaining + TimingEpsilon < durationLeft)
        {
            segmentElapsed += remaining;
            FillAmount = Lerp(
                Ratio(segmentStart),
                Ratio(segmentTarget),
                segmentElapsed / segmentDuration
            );
            remaining = 0f;
            return;
        }

        remaining -= Max(0f, durationLeft);
        displayedExperience = segmentTarget;
        FillAmount = segmentReachesFull
            ? 1f
            : Ratio(displayedExperience);

        if (!segmentReachesFull)
        {
            StartNextSegment();
            return;
        }

        ApplyQueuedLevelOrCancel();
    }

    private void AdvanceHold(ref float remaining)
    {
        float durationLeft = fullHoldDuration - holdElapsed;

        if (remaining + TimingEpsilon < durationLeft)
        {
            holdElapsed += remaining;
            FillAmount = 1f;
            remaining = 0f;
            return;
        }

        remaining -= Max(0f, durationLeft);
        displayedExperience = 0;
        FillAmount = 0f;
        holdElapsed = 0f;
        StartNextSegment();
    }

    private void ApplyQueuedLevelOrCancel()
    {
        if (pendingLevels.Count == 0)
        {
            CancelToAuthoritativeState(
                projectedExperience,
                requiredExperience,
                projectedLevel
            );
            return;
        }

        DisplayedLevel = pendingLevels.Dequeue();
        CurrentPhase = Phase.HoldingFull;
        holdElapsed = 0f;
    }

    private void StartNextSegment()
    {
        if (pendingExperience <= 0)
        {
            CurrentPhase = Phase.Idle;
            segmentElapsed = 0f;
            return;
        }

        int segmentExperience = Min(
            pendingExperience,
            requiredExperience - displayedExperience
        );

        if (segmentExperience <= 0)
        {
            CurrentPhase = Phase.Idle;
            segmentElapsed = 0f;
            return;
        }

        segmentStart = displayedExperience;
        segmentTarget = displayedExperience + segmentExperience;
        pendingExperience -= segmentExperience;
        segmentReachesFull = segmentTarget >= requiredExperience;
        segmentElapsed = 0f;
        segmentDuration = Max(
            MinimumSegmentDuration,
            fullBarDuration * segmentExperience /
            requiredExperience
        );
        CurrentPhase = Phase.Filling;
    }

    private void CancelToAuthoritativeState(
        int currentExperience,
        int requiredExperience,
        int level
    )
    {
        this.requiredExperience = Max(1, requiredExperience);
        displayedExperience = Clamp(
            currentExperience,
            0,
            this.requiredExperience - 1
        );
        DisplayedLevel = Max(1, level);
        FillAmount = Ratio(displayedExperience);
        pendingExperience = 0;
        pendingLevels.Clear();
        projectedExperience = displayedExperience;
        projectedLevel = DisplayedLevel;
        CurrentPhase = Phase.Idle;
        segmentElapsed = 0f;
        holdElapsed = 0f;
    }

    private float Ratio(int experience)
    {
        return (float)experience / requiredExperience;
    }

    private static float Lerp(float start, float end, float t)
    {
        t = t < 0f ? 0f : t > 1f ? 1f : t;
        return start + (end - start) * t;
    }

    private static float PositiveOrDefault(
        float value,
        float defaultValue
    )
    {
        return value > 0f &&
               value < float.PositiveInfinity
            ? value
            : defaultValue;
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    private static int Max(int left, int right)
    {
        return left > right ? left : right;
    }

    private static float Max(float left, float right)
    {
        return left > right ? left : right;
    }

    private static int Min(int left, int right)
    {
        return left < right ? left : right;
    }
}
