
using UnityEngine;
using Fusion;
using Bombanana.Interaction;

public static class HandRotationCodec
{
    private const float Range = 0.70710678118f;

    public static uint Pack(Quaternion rotation)
    {
        Quaternion q = Quaternion.Normalize(rotation);
        float[] components = { q.x, q.y, q.z, q.w };

        int largest = 0;
        for (int i = 1; i < 4; i++)
            if (Mathf.Abs(components[i]) > Mathf.Abs(components[largest]))
                largest = i;

        float sign = components[largest] < 0f ? -1f : 1f;
        uint packed = (uint)largest << 30;
        int shift = 20;

        for (int i = 0; i < 4; i++)
        {
            if (i == largest) continue;

            float value = components[i] * sign;
            uint quantized = (uint)Mathf.Clamp(Mathf.RoundToInt((value / Range + 1f) * 511.5f), 0, 1023);

            packed |= quantized << shift;
            shift -= 10;
        }

        return packed;
    }

    public static Quaternion Unpack(uint packed)
    {
        int largest = (int)(packed >> 30);
        float[] components = new float[4];
        float sum = 0f;
        int shift = 20;

        for (int i = 0; i < 4; i++)
        {
            if (i == largest) continue;

            float value = (((packed >> shift) & 1023u) / 1023f * 2f - 1f) * Range;
            components[i] = value;
            sum += value * value;
            shift -= 10;
        }

        components[largest] = Mathf.Sqrt(Mathf.Max(0f, 1f - sum));

        return Quaternion.Normalize(new Quaternion(
            components[0], components[1], components[2], components[3]
        ));
    }
}


public static unsafe class HandTrackingSync
{
    public const int BoneCount = PlayerInputData.MaxHandBones;

    public static unsafe void Capture(CustomHandVisual hand, ref PlayerInputData data, bool isLeft)
    {
        bool valid = hand != null && hand.Root != null && hand.IsVisible;

        if (isLeft) data.leftHandVisible = valid;
        else data.rightHandVisible = valid;

        fixed (uint* left = data.leftBones, right = data.rightBones)
        {
            uint* bones = isLeft ? left : right;

            for (int i = 0; i < BoneCount; i++)
                bones[i] = 0u;

            if (!valid) return;

            var joints = hand.Joints;
            int count = Mathf.Min(joints.Count, BoneCount);

            for (int i = 0; i < count; i++)
            {
                Transform bone = joints[i]?.bone;
                if (bone == null || bone == hand.Root) continue;

                bones[i] = HandRotationCodec.Pack(bone.localRotation);
            }
        }

        Transform orient = hand.OrientationRoot;
        if (orient == null) return;

        Vector3 pos = hand.Root.InverseTransformPoint(orient.position);
        Quaternion rot = Quaternion.Inverse(hand.Root.rotation) * orient.rotation;

        if (isLeft)
        {
            data.leftOrientPosition = pos;
            data.leftOrientRotation = rot;
        }
        else
        {
            data.rightOrientPosition = pos;
            data.rightOrientRotation = rot;
        }
    }

    public static void Apply(CustomHandVisual hand, NetworkArray<uint> bones,
        Vector3 orientPosition, Quaternion orientRotation, bool visible)
    {
        if (hand == null) return;

        if (hand.Renderer != null) hand.Renderer.enabled = visible;
        if (!visible || hand.Root == null) return;

        Transform orient = hand.OrientationRoot;

        if (orient != null && orient != hand.Root && orientRotation != default)
        {
            orient.SetPositionAndRotation(
                hand.Root.TransformPoint(orientPosition),
                hand.Root.rotation * orientRotation
            );
        }

        var joints = hand.Joints;
        int count = Mathf.Min(joints.Count, BoneCount);

        for (int i = 0; i < count; i++)
        {
            Transform bone = joints[i]?.bone;
            uint packed = bones[i];

            if (bone == null || bone == hand.Root || packed == 0u) continue;

            bone.localRotation = HandRotationCodec.Unpack(packed);
        }
    }
}
