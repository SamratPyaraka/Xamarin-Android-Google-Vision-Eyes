using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Gms.Vision.Faces;
using Java.Util;
using Java.Lang;
using Android.Graphics;
using static Android.Gms.Vision.Detector;
using Android.Gms.Vision;
using System.Threading.Tasks;
using GoogleEyes.CustomControls;

namespace GoogleEyes
{
    public class GooglyFaceTracker: Tracker,  CameraSource.IPictureCallback
    {
        private static readonly float EYE_CLOSED_THRESHOLD = 0.4f;

        private GraphicOverlay mOverlay;
        private GooglyEyesGraphic mEyesGraphic;
        private CameraSource mCameraSource = null;
        private bool isProcessing = false;


        // Record the previously seen proportions of the landmark locations relative to the bounding box
        // of the face.  These proportions can be used to approximate where the landmarks are within the
        // face bounding box if the eye landmark is missing in a future update.
        private IMap mPreviousProportions = new HashMap();

        //private Map<Integer, PointF> mPreviousProportions = new HashMap(,)

        // Similarly, keep track of the previous eye open state so that it can be reused for
        // intermediate frames which lack eye landmarks and corresponding eye state.
        private bool mPreviousIsLeftOpen = true;
        private bool mPreviousIsRightOpen = true;

        public GooglyFaceTracker(GraphicOverlay overlay, CameraSource cameraSource = null)
        {
            mOverlay = overlay;
            mEyesGraphic = new GooglyEyesGraphic(mOverlay);
            mCameraSource = cameraSource;
        }

        public override void OnNewItem(int id, Java.Lang.Object item)
        {
            mEyesGraphic.SetId(id);
            mEyesGraphic = new GooglyEyesGraphic(mOverlay);
        }

        public override void OnUpdate(Detections detectionResults, Java.Lang.Object item)
        {
            mOverlay.Add(mEyesGraphic);
            var face = item as Face;
            updatePreviousProportions(item);

            PointF leftPosition = getLandmarkPosition(face, (int)LandmarkType.LeftEye);
            PointF rightPosition = getLandmarkPosition(face, (int)LandmarkType.RightEye);

            float leftOpenScore = face.IsLeftEyeOpenProbability;
            bool isLeftOpen;
            if (leftOpenScore == Face.UncomputedProbability)
            {
                isLeftOpen = mPreviousIsLeftOpen;
            }
            else
            {
                isLeftOpen = (leftOpenScore > EYE_CLOSED_THRESHOLD);
                mPreviousIsLeftOpen = isLeftOpen;
            }

            float rightOpenScore = face.IsRightEyeOpenProbability;
            bool isRightOpen;
            if (rightOpenScore == Face.UncomputedProbability)
            {
                isRightOpen = mPreviousIsRightOpen;
            }
            else
            {
                isRightOpen = (rightOpenScore > EYE_CLOSED_THRESHOLD);
                mPreviousIsRightOpen = isRightOpen;
            }

            mEyesGraphic.updateEyes(leftPosition, isLeftOpen, rightPosition, isRightOpen);
        }

        public override void OnMissing(Detections detectionResults)
        {
            mOverlay.Remove(mEyesGraphic);
        }

        /**
         * Called when the face is assumed to be gone for good. Remove the googly eyes graphic from
         * the overlay.
         */
        
        public override void OnDone()
        {
            mOverlay.Remove(mEyesGraphic);
        }

        //==============================================================================================
        // Private
        //==============================================================================================

        private void updatePreviousProportions(Java.Lang.Object item)
        {
            var face = item as Face;
            foreach (Landmark landmark in face.Landmarks)
            {
                PointF position = landmark.Position;
                float xProp = (position.X - face.Position.X) / face.Width;
                float yProp = (position.Y - face.Position.Y) / face.Width;
                mPreviousProportions.Put((int)landmark.Type, new PointF(xProp, yProp));
            }
        }

        /**
         * Finds a specific landmark position, or approximates the position based on past observations
         * if it is not present.
         */
        private PointF getLandmarkPosition(Face face, int landmarkId)
        {
            foreach (Landmark landmark in face.Landmarks)
            {
                if ((int)landmark.Type == landmarkId)
                {
                    return landmark.Position;
                }
            }

            PointF prop = (PointF)mPreviousProportions.Get(landmarkId);
            if (prop == null)
            {
                return null;
            }

            float x = face.Position.X + (prop.X * face.Width);
            float y = face.Position.Y + (prop.Y * face.Height);
            return new PointF(x, y);
        }

        public void OnPictureTaken(byte[] data)
        {
            Task.Run(async () =>
            {
                try
                {
                    isProcessing = true;

                    System.Console.WriteLine("face Eyes detected: ");
                    

                }

                finally
                {
                    isProcessing = false;


                }

            });
        }
    }
}