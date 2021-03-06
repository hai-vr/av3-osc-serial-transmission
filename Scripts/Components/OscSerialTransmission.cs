#if UNITY_EDITOR
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using AnimatorAsCode.V0;
using AnimatorAsCodeFramework.Examples;
using UnityEditor;

namespace Hai.OscSerialTransmission.Scripts.Components
{
    public class OscSerialTransmission : MonoBehaviour
    {
        public VRCAvatarDescriptor avatar;
        public AnimatorController assetContainer;
        public string assetKey;
        public string layerName;
        public string clockParameter;
        public string dataParameter;
        public int numberOfBitsPerMessage;
        public string normalizedOutputParam;
        public string dataVizParameter;
        public string clockVizParameter;
    }

    [CustomEditor(typeof(OscSerialTransmission), true)]
    public class OscSerialTransmissionImpl : Editor
    {
        private const string ClockHasChangedParam = "SERIAL_SYNC";

        public override void OnInspectorGUI()
        {
            AacExample.InspectorTemplate(this, serializedObject, "assetKey", Create);
        }

        private void Create()
        {
            var my = (OscSerialTransmission) target;
            if (string.IsNullOrWhiteSpace(my.layerName))
            {
                return;
            }
            var aac = AacExample.AnimatorAsCode(my.layerName, my.avatar, my.assetContainer, my.assetKey, AacExample.Options().WriteDefaultsOff());

            // FIXME: We only need one clock layer, regardless of the number of independent serial buses we have.
            CreateClockLayer(aac, my);

            CreateSerialLayer(aac, my);
        }

        private static void CreateClockLayer(AacFlBase aac, OscSerialTransmission my)
        {
            var clkChange = aac.CreateSupportingFxLayer("ClkChange");
            var clkParam = clkChange.BoolParameter(my.clockParameter);
            var syncParam = clkChange.BoolParameter(ClockHasChangedParam);
            var clockViz = clkChange.FloatParameter(my.clockVizParameter);

            // It is said that transitions are faster if they use a "True" boolean condition
            // rather than Exit Time. I have never verified this.
            var alwaysTrue = clkChange.BoolParameter("SERIAL_TRUE");
            clkChange.OverrideValue(alwaysTrue, true);

            var aapDataZero = aac.NewClip()
                .Animating(clip =>
                {
                    clip.AnimatesAnimator(clockViz).WithOneFrame(0f);
                });
            var aapDataOne = aac.NewClip()
                .Animating(clip =>
                {
                    clip.AnimatesAnimator(clockViz).WithOneFrame(1f);
                });

            var clkIdle = clkChange.NewState("Idle");
            var low = clkChange.NewState("Low").RightOf().Drives(syncParam, true).WithAnimation(aapDataZero);
            var lowRem = clkChange.NewState("LowRem").Drives(syncParam, false).WithAnimation(aapDataZero);
            var high = clkChange.NewState("High").RightOf().Drives(syncParam, true).WithAnimation(aapDataOne);
            var highRem = clkChange.NewState("HighRem").Drives(syncParam, false).WithAnimation(aapDataOne);

            clkIdle.TransitionsTo(lowRem).When(clkParam.IsFalse());
            lowRem.TransitionsTo(high).When(clkParam.IsTrue());
            high.TransitionsTo(highRem).When(alwaysTrue.IsTrue());
            highRem.TransitionsTo(low).When(clkParam.IsFalse());
            low.TransitionsTo(lowRem).When(alwaysTrue.IsTrue());
        }

        private static void CreateSerialLayer(AacFlBase aac, OscSerialTransmission my)
        {
            var serial = aac.CreateSupportingFxLayer("Serial");
            var syncParam = serial.BoolParameter(ClockHasChangedParam);
            var clockHasChanged = syncParam.IsTrue();
            var data = serial.BoolParameter(my.dataParameter);
            var message = serial.FloatParameter(my.normalizedOutputParam);
            var messageBuffer = serial.FloatParameter($"{my.normalizedOutputParam}_BUFFER");
            var dataViz = serial.FloatParameter(my.dataVizParameter);

            // Avatar has loaded, waiting for initial state.
            // The avatar may load while a message is being transferred.
            var noComms = serial.NewState("NoComms");
            var idle = serial.NewState("Idle");
            noComms.TransitionsTo(idle).When(data.IsTrue()); // Avatar has just loaded.

            var aapDataZero = aac.NewClip()
                .Animating(clip =>
                {
                    clip.AnimatesAnimator(message).WithFrameCountUnit(keyframes => keyframes.Linear(0, 0f).Linear(60, 1f));
                    clip.AnimatesAnimator(dataViz).WithOneFrame(0f);
                });
            var aapDataOne = aac.NewClip()
                .Animating(clip =>
                {
                    clip.AnimatesAnimator(message).WithFrameCountUnit(keyframes => keyframes.Linear(0, 0f).Linear(60, 1f));
                    clip.AnimatesAnimator(dataViz).WithOneFrame(1f);
                });

            // Begin communication.
            var beginComms = serial.NewState("Synchronization").RightOf()
                .WithAnimation(aapDataZero).MotionTime(message)
                .Drives(messageBuffer, 0);
            idle.TransitionsTo(beginComms).When(clockHasChanged).And(data.IsFalse());

            var maximumRepresentableNumber = Mathf.Pow(2, my.numberOfBitsPerMessage) - 1;

            AacFlState[] previousMutable = {beginComms};
            // Least significant bit first
            for (var i = 0; i < my.numberOfBitsPerMessage; i++)
            {
                var high = serial.NewState($"High {i}")
                    .WithAnimation(aapDataOne).MotionTime(message)
                    .DrivingIncreases(messageBuffer, Mathf.Pow(2, i) / maximumRepresentableNumber)
                    .Shift(beginComms, i + 1, 0);
                var low = serial.NewState($"Low {i}")
                    .WithAnimation(aapDataZero).MotionTime(message);

                foreach (var previous in previousMutable)
                {
                    previous.TransitionsTo(low).When(clockHasChanged).And(data.IsFalse());
                    previous.TransitionsTo(high).When(clockHasChanged).And(data.IsTrue());
                }

                // Must be last statement in this loop
                previousMutable = new[] {low, high};
            }

            var parityBit = serial.NewState("Parity (IGNORED)").RightOf()
                .WithAnimation(aapDataZero).MotionTime(message);
            foreach (var mostSignificantBit in previousMutable)
            {
                mostSignificantBit.TransitionsTo(parityBit).When(clockHasChanged);
            }

            var stop = serial.NewState("Stop").RightOf()
                .WithAnimation(aapDataOne).MotionTime(messageBuffer); // Magic plays here, this copies the buffer to the AAP
            parityBit.TransitionsTo(stop).When(clockHasChanged).And(data.IsTrue());

            stop.TransitionsTo(beginComms).When(clockHasChanged).And(data.IsFalse());
        }
    }
}
#endif