#if UNITY_EDITOR
using AnimatorAsCodeFramework.Examples;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using VRC.SDK3.Avatars.Components;

namespace Hai.OscSerialTransmission.Scripts.Components
{
    public class OscSerialDemo : MonoBehaviour
    {
        public VRCAvatarDescriptor avatar;
        public AnimatorController assetContainer;
        public string assetKey;

        public string normalizedOutputParam;
        public string clockVizParameter;
        public string dataVizParameter;
        public PositionConstraint clkParticleConstraint;
        public PositionConstraint dataParticleConstraint;
        public MeshRenderer valueVizRenderer;
        public MeshRenderer bufferVizRenderer;
        public int numberOfBits;
    }

    [CustomEditor(typeof(OscSerialDemo), true)]
    public class OscSerialDemoImpl : Editor
    {
        public override void OnInspectorGUI()
        {
            AacExample.InspectorTemplate(this, serializedObject, "assetKey", Create);
        }

        private void Create()
        {
            var my = (OscSerialDemo) target;
            var maxRepresentableValue = Mathf.Pow(2, my.numberOfBits) - 1;

            var aac = AacExample.AnimatorAsCode("OscDemo", my.avatar, my.assetContainer, my.assetKey, AacExample.Options().WriteDefaultsOff());

            var clkViz = aac.CreateSupportingFxLayer("ClkViz");
            clkViz.NewState("Motion")
                .MotionTime(clkViz.FloatParameter(my.clockVizParameter))
                .WithAnimation(aac.NewClip().Animating(clip =>
                {
                    clip.Animates(my.clkParticleConstraint, "m_Weight").WithFrameCountUnit(keyframes => keyframes.Linear(0, 0).Linear(60, 1f));
                }));
            var dataViz = aac.CreateSupportingFxLayer("DataViz");
            dataViz.NewState("Motion")
                .MotionTime(dataViz.FloatParameter(my.dataVizParameter))
                .WithAnimation(aac.NewClip().Animating(clip =>
                {
                    clip.Animates(my.dataParticleConstraint, "m_Weight").WithFrameCountUnit(keyframes => keyframes.Linear(0, 0).Linear(60, 1f));
                }));

            var valueViz = aac.CreateSupportingFxLayer("ValueViz");
            valueViz.NewState("Motion")
                .MotionTime(valueViz.FloatParameter(my.normalizedOutputParam))
                .WithAnimation(aac.NewClip().Animating(clip =>
                {
                    clip.Animates(my.valueVizRenderer, "material._Value").WithFrameCountUnit(keyframes => keyframes.Linear(0, 0).Linear(60, maxRepresentableValue));
                }));

            var bufferViz = aac.CreateSupportingFxLayer("BufferViz");
            bufferViz.NewState("Motion")
                .MotionTime(bufferViz.FloatParameter($"{my.normalizedOutputParam}_BUFFER"))
                .WithAnimation(aac.NewClip().Animating(clip =>
                {
                    clip.Animates(my.bufferVizRenderer, "material._Value").WithFrameCountUnit(keyframes => keyframes.Linear(0, 0).Linear(60, maxRepresentableValue));
                }));

        }
    }
}
#endif