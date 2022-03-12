using System;
using AnimatorAsCode.V0;
using AnimatorAsCodeFramework.Examples;
using UnityEditor;
using UnityEngine;

namespace Hai.OscSerialTransmission.Scripts.Editor
{
    [CustomEditor(typeof(global::Hai.OscSerialTransmission.Scripts.Components.OscSerialTransmission), true)]
    public class OscSerialTransmissionImpl : UnityEditor.Editor
    {
        private const string ClockHasChangedParam = "SERIAL_SYNC";

        public override void OnInspectorGUI()
        {
            AacExample.InspectorTemplate(this, serializedObject, "assetKey", Create);
        }

        private void Create()
        {
            var my = (global::Hai.OscSerialTransmission.Scripts.Components.OscSerialTransmission) target;
            if (string.IsNullOrWhiteSpace(my.layerName))
            {
                return;
            }
            var aac = AacExample.AnimatorAsCode(my.layerName, my.avatar, my.assetContainer, my.assetKey, AacExample.Options().WriteDefaultsOff());

            // FIXME: We only need one clock layer, regardless of the number of independent serial buses we have.
            CreateClockLayer(aac, my);

            CreateSerialLayer(aac, my);
        }

        private static void CreateClockLayer(AacFlBase aac, Components.OscSerialTransmission my)
        {
            var clkChange = aac.CreateSupportingFxLayer("ClkChange");
            var clkParam = clkChange.BoolParameter(my.clockParameter);
            var syncParam = clkChange.BoolParameter(ClockHasChangedParam);

            // It is said that transitions are faster if they use a "True" boolean condition
            // rather than Exit Time. I have never verified this.
            var alwaysTrue = clkChange.BoolParameter("SERIAL_TRUE");
            clkChange.OverrideValue(alwaysTrue, true);

            var clkIdle = clkChange.NewState("Idle");
            var low = clkChange.NewState("Low").RightOf().Drives(syncParam, true);
            var lowRem = clkChange.NewState("LowRem").Drives(syncParam, false);
            var high = clkChange.NewState("High").RightOf().Drives(syncParam, true);
            var highRem = clkChange.NewState("HighRem").Drives(syncParam, false);

            clkIdle.TransitionsTo(lowRem).When(clkParam.IsFalse());
            lowRem.TransitionsTo(high).When(clkParam.IsTrue());
            high.TransitionsTo(highRem).When(alwaysTrue.IsTrue());
            highRem.TransitionsTo(low).When(clkParam.IsFalse());
            low.TransitionsTo(lowRem).When(alwaysTrue.IsTrue());
        }

        private static void CreateSerialLayer(AacFlBase aac, Components.OscSerialTransmission my)
        {
            var serial = aac.CreateSupportingFxLayer("Serial");
            var syncParam = serial.BoolParameter(ClockHasChangedParam);
            var clockHasChanged = syncParam.IsTrue();
            var data = serial.BoolParameter(my.dataParameter);
            var message = serial.FloatParameter($"{my.normalizedOutputParam}");
            var messageIsBeta = serial.BoolParameter($"{my.normalizedOutputParam}_SWAP");
            var messageBufferAlpha = serial.FloatParameter($"{my.normalizedOutputParam}_BUFFER_A");
            var messageBufferBeta = serial.FloatParameter($"{my.normalizedOutputParam}_BUFFER_B");

            // Avatar has loaded, waiting for initial state.
            // The avatar may load while a message is being transferred.
            var noComms = serial.NewState("NoComms");
            var idle = serial.NewState("Idle");
            noComms.TransitionsTo(idle).When(data.IsTrue()); // Avatar has just loaded.

            // Begin communication.
            var beginComms = serial.NewState("Synchronization (ALPHA)").RightOf()
                .Drives(messageBufferAlpha, 0);
            idle.TransitionsTo(beginComms).When(clockHasChanged).And(data.IsFalse());

            var maximumRepresentableNumber = Mathf.Pow(2, my.numberOfBitsPerMessage) - 1;

            AacFlState[] previousMutable = {beginComms};
            // Least significant bit first
            bool swap = false;
            for (var i = 0; i < my.numberOfBitsPerMessage; i++)
            {
                var high = serial.NewState($"High {i}")
                    .DrivingIncreases(messageBufferAlpha, Mathf.Pow(2, i) / maximumRepresentableNumber)
                    .DrivingLocally()
                    .Shift(beginComms, i + 1, 0);
                var low = serial.NewState($"Low {i}");

                foreach (var previous in previousMutable)
                {
                    previous.TransitionsTo(low).When(clockHasChanged).And(data.IsTrue());
                    previous.TransitionsTo(high).When(clockHasChanged).And(data.IsFalse());
                }

                // Must be last statement in this loop
                previousMutable = new[] {low, high};
            }

            var parityBit = serial.NewState("Parity (IGNORED)").RightOf();
            foreach (var mostSignificantBit in previousMutable)
            {
                mostSignificantBit.TransitionsTo(parityBit).When(clockHasChanged);
            }

            var stop = serial.NewState("Stop (ALPHA)").RightOf();
            parityBit.TransitionsTo(stop).When(clockHasChanged).And(data.IsTrue());

            stop.TransitionsTo(beginComms).When(clockHasChanged).And(data.IsFalse());
        }
    }
}
