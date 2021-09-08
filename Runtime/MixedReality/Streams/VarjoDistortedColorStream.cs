// Copyright 2019 Varjo Technologies Oy. All rights reserved.

using System;
using UnityEngine;

namespace Varjo.XR
{
    public class VarjoDistortedColorStream : VarjoFrameStream
    {
        public class VarjoDistortedColorFrame
        {
            public long timestamp { get; internal set; }                   //!< Timestamp at end of exposure.
            public VarjoCameraMetadata metadata { get; internal set; }     //!< Camera metadata
            public Texture2D leftTexture { get; internal set; }            //!< Texture from left camera.
            public Texture2D rightTexture { get; internal set; }           //!< Texture from right camera.
        }

        private VarjoDistortedColorData data;
        private VarjoTextureBuffer leftBuffer;
        private VarjoTextureBuffer rightBuffer;

        public VarjoCameraIntrinsics leftIntrinsics;
        public VarjoCameraIntrinsics rightIntrinsics;

        internal VarjoDistortedColorStream() : base()
        {
            leftBuffer = new VarjoTextureBuffer(false);
            rightBuffer = new VarjoTextureBuffer(false);
        }

        /// <summary>
        /// Gets latest frame from the frame stream.
        /// Frames update only if stream has been started.
        /// May be called from main thread only.
        /// </summary>
        /// <returns>Latest Distorted color stream frame.</returns>
        public VarjoDistortedColorFrame GetFrame()
        {
            lock (mutex)
            {
                var frame = new VarjoDistortedColorFrame();
                frame.timestamp = data.timestamp;
                frame.metadata = new VarjoCameraMetadata(data);
                frame.leftTexture = leftBuffer.GetTexture2D();
                frame.rightTexture = rightBuffer.GetTexture2D();
                return frame;
            }
        }

        internal override void NewFrameCallback(VarjoStreamFrame streamData, IntPtr userdata)
        {
            lock (mutex)
            {
                Debug.Assert(streamData.type == StreamType);
                data = streamData.metadata.distortedColorData;

                long leftBufferId = 0;
                if (!VarjoMixedReality.GetDataStreamBufferId(streamData.id, streamData.frameNumber, 0 /* varjo_ChannelIndex_Left */, out leftBufferId))
                {
                    Debug.LogErrorFormat("Failed to get distorted color left buffer id {0}", streamData.frameNumber);
                    return;
                }
                leftIntrinsics = VarjoMixedReality.GetCameraIntrinsics(streamData.id, streamData.frameNumber, 0);

                long rightBufferId = 0;
                if (!VarjoMixedReality.GetDataStreamBufferId(streamData.id, streamData.frameNumber, 1/* varjo_ChannelIndex_Right */, out rightBufferId))
                {
                    Debug.LogErrorFormat("Failed to get distorted color right buffer id {0}", streamData.frameNumber);
                    return;
                }
                rightIntrinsics = VarjoMixedReality.GetCameraIntrinsics(streamData.id, streamData.frameNumber, 1);

                leftBuffer.UpdateBuffer(leftBufferId);
                rightBuffer.UpdateBuffer(rightBufferId);
            }
        }

        internal override VarjoStreamType StreamType { get { return VarjoStreamType.DistortedColor; } }
    }
}
