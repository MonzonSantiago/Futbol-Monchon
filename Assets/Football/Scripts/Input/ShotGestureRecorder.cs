using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// High-frequency continuous mouse recorder.
///
/// OLD DESIGN:
///
/// LMB down
///     -> start recording
///
/// LMB up
///     -> stop recording
///
/// That design is no longer used.
///
/// ---------------------------------------------------------
///
/// CURRENT DESIGN:
///
/// The recorder continuously keeps a short circular history
/// of raw mouse movement.
///
/// When the real shot contact happens, another component asks:
///
/// "Give me all mouse movement between time A and time B."
///
/// This allows us to capture:
///
/// a few milliseconds BEFORE release
///         +
/// release -> physical ball contact
///
/// without recording the entire LMB charge gesture.
///
/// ---------------------------------------------------------
///
/// This component:
///
/// - does NOT know shot power
/// - does NOT know the ball
/// - does NOT know camera aim
/// - does NOT calculate curve
/// - does NOT move anything
///
/// It only records raw mouse motion at the highest practical
/// frequency provided by Unity Input System.
/// </summary>
public class ShotGestureRecorder : MonoBehaviour
{
    // =========================================================
    // BUFFER
    // =========================================================

    [Header("Continuous Buffer")]

    [Tooltip(
        "Maximum amount of raw mouse events retained. " +
        "16384 is far more than required for a short contact window."
    )]
    public int bufferCapacity = 16384;

    [Tooltip(
        "Ignore mouse deltas smaller than this magnitude. " +
        "Keep at zero while developing."
    )]
    public float minimumDeltaMagnitude = 0f;

    // =========================================================
    // HIGH FREQUENCY
    // =========================================================

    [Header("High Frequency")]

    [Tooltip(
        "Prevents Unity Input System from merging redundant " +
        "high-frequency mouse events."
    )]
    public bool disableMouseEventMerging = true;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    public bool logCapturedWindows = true;

    // =========================================================
    // INTERNAL SAMPLE
    // =========================================================

    private struct BufferedMouseSample
    {
        public double timestamp;
        public Vector2 delta;

        public BufferedMouseSample(
            double timestamp,
            Vector2 delta
        )
        {
            this.timestamp =
                timestamp;

            this.delta =
                delta;
        }
    }

    // =========================================================
    // RING BUFFER
    // =========================================================

    private BufferedMouseSample[] buffer;

    /// <summary>
    /// Index of the oldest valid sample.
    /// </summary>
    private int startIndex;

    /// <summary>
    /// Amount of currently valid samples.
    /// </summary>
    private int sampleCount;

    private bool previousEventMergingSetting;

    // =========================================================
    // PUBLIC DEBUG INFORMATION
    // =========================================================

    public int BufferedSampleCount
    {
        get { return sampleCount; }
    }

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        bufferCapacity =
            Mathf.Max(
                bufferCapacity,
                1024
            );

        buffer =
            new BufferedMouseSample[
                bufferCapacity
            ];

        startIndex =
            0;

        sampleCount =
            0;
    }

    private void OnEnable()
    {
        previousEventMergingSetting =
            InputSystem.settings
                .disableRedundantEventsMerging;

        if (disableMouseEventMerging)
        {
            InputSystem.settings
                .disableRedundantEventsMerging =
                true;
        }

        InputSystem.onEvent +=
            OnInputEvent;
    }

    private void OnDisable()
    {
        InputSystem.onEvent -=
            OnInputEvent;

        InputSystem.settings
            .disableRedundantEventsMerging =
            previousEventMergingSetting;
    }

    // =========================================================
    // RAW INPUT
    // =========================================================

    private void OnInputEvent(
        InputEventPtr eventPtr,
        InputDevice device
    )
    {
        Mouse mouse =
            device as Mouse;

        if (mouse == null)
            return;

        bool stateEvent =
            eventPtr.IsA<StateEvent>();

        bool deltaStateEvent =
            eventPtr.IsA<DeltaStateEvent>();

        if (
            !stateEvent &&
            !deltaStateEvent
        )
        {
            return;
        }

        bool containsDelta =
            mouse.delta.ReadValueFromEvent(
                eventPtr,
                out Vector2 delta
            );

        if (!containsDelta)
            return;

        if (
            delta.magnitude <
            minimumDeltaMagnitude
        )
        {
            return;
        }

        AddSample(
            eventPtr.time,
            delta
        );
    }

    // =========================================================
    // ADD SAMPLE
    // =========================================================

    private void AddSample(
        double timestamp,
        Vector2 delta
    )
    {
        if (
            buffer == null ||
            buffer.Length != bufferCapacity
        )
        {
            RebuildBuffer();
        }

        int writeIndex;

        if (
            sampleCount <
            buffer.Length
        )
        {
            writeIndex =
                (
                    startIndex +
                    sampleCount
                ) %
                buffer.Length;

            sampleCount++;
        }
        else
        {
            /*
             * Buffer is full.
             *
             * Overwrite the oldest sample and advance
             * the beginning by one position.
             */
            writeIndex =
                startIndex;

            startIndex =
                (
                    startIndex + 1
                ) %
                buffer.Length;
        }

        buffer[writeIndex] =
            new BufferedMouseSample(
                timestamp,
                delta
            );
    }

    // =========================================================
    // EXTRACT CONTACT WINDOW
    // =========================================================

    /// <summary>
    /// Copies all raw mouse movement inside the specified
    /// timestamp interval.
    ///
    /// The resulting ShotGestureSample.time value is relative
    /// to windowStartTime.
    ///
    /// Returns amount of samples copied.
    /// </summary>
    public int CaptureWindow(
        double windowStartTime,
        double windowEndTime,
        List<ShotGestureSample> destination,
        out Vector2 displacement,
        out float travelDistance
    )
    {
        if (destination == null)
        {
            displacement =
                Vector2.zero;

            travelDistance =
                0f;

            return 0;
        }

        destination.Clear();

        displacement =
            Vector2.zero;

        travelDistance =
            0f;

        if (
            sampleCount == 0 ||
            buffer == null
        )
        {
            return 0;
        }

        Vector2 cumulative =
            Vector2.zero;

        for (
            int i = 0;
            i < sampleCount;
            i++
        )
        {
            int index =
                (
                    startIndex + i
                ) %
                buffer.Length;

            BufferedMouseSample sample =
                buffer[index];

            if (
                sample.timestamp <
                windowStartTime
            )
            {
                continue;
            }

            if (
                sample.timestamp >
                windowEndTime
            )
            {
                break;
            }

            cumulative +=
                sample.delta;

            travelDistance +=
                sample.delta.magnitude;

            float relativeTime =
                Mathf.Max(
                    0f,
                    (float)(
                        sample.timestamp -
                        windowStartTime
                    )
                );

            destination.Add(
                new ShotGestureSample(
                    relativeTime,
                    sample.delta,
                    cumulative
                )
            );
        }

        displacement =
            cumulative;

        if (logCapturedWindows)
        {
            Debug.Log(
                "[SHOT CONTACT RAW] " +
                "Samples: " +
                destination.Count +
                " | Window: " +
                (
                    windowEndTime -
                    windowStartTime
                ).ToString("F4") +
                " s" +
                " | Displacement: " +
                displacement +
                " | Travel: " +
                travelDistance.ToString("F2")
            );
        }

        return destination.Count;
    }

    // =========================================================
    // BUFFER MANAGEMENT
    // =========================================================

    private void RebuildBuffer()
    {
        bufferCapacity =
            Mathf.Max(
                bufferCapacity,
                1024
            );

        buffer =
            new BufferedMouseSample[
                bufferCapacity
            ];

        startIndex =
            0;

        sampleCount =
            0;
    }
}