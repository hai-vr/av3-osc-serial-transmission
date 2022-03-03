using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

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
    }
}
