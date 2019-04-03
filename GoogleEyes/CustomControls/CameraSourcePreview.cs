using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Vision;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.IO;

namespace GoogleEyes.CustomControls
{
    public sealed class CameraSourcePreview : ViewGroup, ISurfaceHolderCallback
    {
        private static readonly string TAG = "CameraSourcePreview";

        private Context mContext;
        private SurfaceView mSurfaceView;
        private bool mStartRequested;
        public bool mSurfaceAvailable;
        private CameraSource mCameraSource;

        private GraphicOverlay mOverlay;

        public CameraSourcePreview(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            mContext = context;
            mStartRequested = false;
            mSurfaceAvailable = false;

            mSurfaceView = new SurfaceView(context);
            mSurfaceView.Holder.AddCallback(this);
            AddView(mSurfaceView);
        }

        public void Start(CameraSource cameraSource)
        {
            try
            {
                if (cameraSource == null)
                {
                    Stop();
                }

                mCameraSource = cameraSource;

                if (mCameraSource != null)
                {
                    mStartRequested = true;
                    startIfReady();
                }
            }
            catch (IOException ex)
            {
            }
        }

        public void Start(CameraSource cameraSource, GraphicOverlay overlay)
        {
            try
            {
                mOverlay = overlay;
                Start(cameraSource);
            }
            catch (IOException ex)
            {

            }

        }

        public void Stop()
        {
            if (mCameraSource != null)
            {
                mCameraSource.Stop();
            }
        }

        public void Release()
        {
            if (mCameraSource != null)
            {
                mCameraSource.Release();
                mCameraSource = null;
            }
        }

        private void startIfReady()
        {
            try
            {
                if (mStartRequested && mSurfaceAvailable)
                {
                    mCameraSource.Start(mSurfaceView.Holder);
                    if (mOverlay != null)
                    {
                        Android.Gms.Common.Images.Size size = mCameraSource.PreviewSize;
                        int min = Math.Min(size.Width, size.Height);
                        int max = Math.Max(size.Width, size.Height);
                        if (IsPortraitMode())
                        {
                            // Swap width and height sizes when in portrait, since it will be rotated by
                            // 90 degrees
                            mOverlay.SetCameraInfo(min, max, mCameraSource.CameraFacing);
                        }
                        else
                        {
                            mOverlay.SetCameraInfo(max, min, mCameraSource.CameraFacing);
                        }
                        mOverlay.Clear();
                    }
                    mStartRequested = false;
                }
            }
            catch (IOException ex)
            {

            }
        }


        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            int previewWidth = 320;
            int previewHeight = 240;
            if (mCameraSource != null)
            {
                Android.Gms.Common.Images.Size size = mCameraSource.PreviewSize;
                if (size != null)
                {
                    previewWidth = size.Width;
                    previewHeight = size.Height;
                }
            }

            // Swap width and height sizes when in portrait, since it will be rotated 90 degrees
            if (IsPortraitMode())
            {
                int tmp = previewWidth;
                previewWidth = previewHeight;
                previewHeight = tmp;
            }

            int viewWidth = right - left;
            int viewHeight = bottom - top;

            int childWidth;
            int childHeight;
            int childXOffset = 0;
            int childYOffset = 0;
            float widthRatio = (float)viewWidth / (float)previewWidth;
            float heightRatio = (float)viewHeight / (float)previewHeight;

            // To fill the view with the camera preview, while also preserving the correct aspect ratio,
            // it is usually necessary to slightly oversize the child and to crop off portions along one
            // of the dimensions.  We scale up based on the dimension requiring the most correction, and
            // compute a crop offset for the other dimension.
            if (widthRatio > heightRatio)
            {
                childWidth = viewWidth;
                childHeight = (int)((float)previewHeight * widthRatio);
                childYOffset = (childHeight - viewHeight) / 2;
            }
            else
            {
                childWidth = (int)((float)previewWidth * heightRatio);
                childHeight = viewHeight;
                childXOffset = (childWidth - viewWidth) / 2;
            }

            for (int i = 0; i < ChildCount; ++i)
            {
                // One dimension will be cropped.  We shift child over or up by this offset and adjust
                // the size to maintain the proper aspect ratio.
                GetChildAt(i).Layout(
                        -1 * childXOffset, -1 * childYOffset,
                        childWidth - childXOffset, childHeight - childYOffset);
            }

            try
            {
                startIfReady();
            }
            catch (IOException e)
            {
                Log.Error(TAG, "Could not start camera source.", e);
            }
        }

        private bool IsPortraitMode()
        {
            int orientation = (int)mContext.Resources.Configuration.Orientation;
            if (orientation == 2) //Landscape
            {
                return false;
            }
            if (orientation == 1) //Portrait
            {
                return true;
            }

            Log.Debug(TAG, "isPortraitMode returning false by default");
            return false;
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            mSurfaceAvailable = true;
            //CameraSourcePreview cameraSource = new CameraSourcePreview();
            try
            {
                startIfReady();
            }
            catch (IOException e)
            {
                Log.Error(TAG, "Could not start camera source.", e);
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            mSurfaceAvailable = false;
        }
    }
}