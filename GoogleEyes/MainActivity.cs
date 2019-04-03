using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Android.Gms.Vision;
using Android.Gms.Vision.Faces;
using Android.Util;
using Android.Gms.Common;
using Java.IO;
using Android;
using Android.Content.PM;
using static Android.Gms.Vision.MultiProcessor;
using Java.Lang;
using Android.Support.V4.App;
using Android.Support.Design.Widget;
using System.Threading.Tasks;
using GoogleEyes.CustomControls;

namespace GoogleEyes
{
    [Activity(Label = "@string/app_name", MainLauncher = true, Theme = "@style/Theme.AppCompat.NoActionBar", ScreenOrientation = ScreenOrientation.FullSensor)]
    public class MainActivity : AppCompatActivity, IFactory
    {
        public const string ActionDeviceStorageLow = "Low";

        private static readonly string TAG = "GooglyEyes";

        private static readonly int RC_HANDLE_GMS = 9001;

        // permission request codes need to be < 256
        private static readonly int RC_HANDLE_CAMERA_PERM = 2;

        private CameraSource mCameraSource = null;
        private CameraSourcePreview mPreview;
        private GraphicOverlay mGraphicOverlay;
        Button button;

        private bool mIsFrontFacing = true;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            mPreview = FindViewById<CameraSourcePreview>(Resource.Id.preview);
            mGraphicOverlay = FindViewById<GraphicOverlay>(Resource.Id.faceOverlay);

            if (savedInstanceState != null)
            {
                mIsFrontFacing = savedInstanceState.GetBoolean("IsFrontFacing");
            }

            if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.Camera) == Permission.Granted)
            {
                createCameraSource();
            }
            else { RequestCameraPermission(); }

            // Create your application here
        }

        private void RequestCameraPermission()
        {
            Log.Warn(TAG, "Camera permission is not granted. Requesting permission");

            var permissions = new string[] { Manifest.Permission.Camera };

            if (!ActivityCompat.ShouldShowRequestPermissionRationale(this,
                    Manifest.Permission.Camera))
            {
                ActivityCompat.RequestPermissions(this, permissions, RC_HANDLE_CAMERA_PERM);
                return;
            }

            Snackbar.Make(mGraphicOverlay, Resource.String.permission_camera_rationale,
                    Snackbar.LengthIndefinite)
                    .SetAction(Resource.String.ok, (o) => { ActivityCompat.RequestPermissions(this, permissions, RC_HANDLE_CAMERA_PERM); })
                    .Show();
        }

        /**
     * Creates the face detector and the camera.
     */
        private void createCameraSource()
        {
            Context context = Application.Context;
            FaceDetector detector = new FaceDetector.Builder(context)
                    .SetClassificationType(ClassificationType.All)
                    .Build();

            detector.SetProcessor(
                    new Builder(this)
                            .Build());

            int facing = (int)CameraFacing.Front;
            if (!mIsFrontFacing)
            {
                facing = (int)CameraFacing.Back;
            }

            mCameraSource = new CameraSource.Builder(context, detector)
                    .SetFacing(CameraFacing.Front)
                    .SetRequestedPreviewSize(640, 480)
                    .SetRequestedFps(60.0f)
                    .SetAutoFocusEnabled(true)
                    .Build();
        }

        protected override void OnResume()
        {
            base.OnResume();

            startCameraSource();
        }

        protected override void OnPause()
        {
            base.OnPause();
            mPreview.Stop();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (mCameraSource != null)
            {
                mCameraSource.Release();
            }
        }

        protected override void OnSaveInstanceState(Bundle savedInstanceState)
        {
            base.OnSaveInstanceState(savedInstanceState);
            savedInstanceState.PutBoolean("IsFrontFacing", mIsFrontFacing);
        }

        private FaceDetector CreateFaceDetector(Context context)
        {

            FaceDetector detector = new FaceDetector.Builder(context)
                    .SetLandmarkType(LandmarkDetectionType.All)
                    .SetClassificationType(ClassificationType.All)
                    .SetTrackingEnabled(true)
                    .SetMode(FaceDetectionMode.Fast)
                    .SetProminentFaceOnly(mIsFrontFacing)
                    .SetMinFaceSize(mIsFrontFacing ? 0.35f : 0.15f)
                    .Build();
            //Detector.IProcessor processor;

            Detector.IProcessor processor;
            if (mIsFrontFacing)
            {
                Tracker tracker = new GooglyFaceTracker(mGraphicOverlay);
                processor = new LargestFaceFocusingProcessor.Builder(detector, tracker).Build();
            }
            else
            {
                /*MultiProcessor.IFactory factory = new MultiProcessor.IFactory(){
                   
                    public override Tracker Create(Face face)
                    {
                        return new GooglyFaceTracker(mGraphicOverlay);
                    }
                };
                processor = new MultiProcessor.Builder(factory).Build();*/
                Tracker tracker = new GooglyFaceTracker(mGraphicOverlay);
                processor = new LargestFaceFocusingProcessor.Builder(detector, tracker).Build();
            }

            detector.SetProcessor(processor);

            if (!detector.IsOperational)
            {
                Log.Warn(TAG, "Face detector dependencies are not yet available.");
                Toast.MakeText(this, Resource.String.low_storage_error, ToastLength.Long).Show();
                /*
                IntentFilter lowStorageFilter = (IntentFilter)ActionDeviceStorageLow;
                bool hasLowStorage = RegisterReceiver(null, lowStorageFilter) != null;

                if (hasLowStorage) {
                    Toast.MakeText(this, Resource.String.low_storage_error, ToastLength.Long).Show();
                    Log.Warn(TAG, GetString(Resource.String.low_storage_error));
                }*/
            }
            return detector;
        }

        private void startCameraSource()
        {
            // check that the device has play services available.
            int code = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(
                    this.ApplicationContext);
            if (code != ConnectionResult.Success)
            {
                Dialog dlg =
                        GoogleApiAvailability.Instance.GetErrorDialog(this, code, RC_HANDLE_GMS);
                dlg.Show();
            }

            if (mCameraSource != null)
            {
                try
                {
                    mPreview.Start(mCameraSource, mGraphicOverlay);
                }
                catch (IOException e)
                {
                    Log.Error(TAG, "Unable to start camera source.", e);
                    mCameraSource.Release();
                    mCameraSource = null;
                }
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode != RC_HANDLE_CAMERA_PERM)
            {
                Log.Debug(TAG, "Got unexpected permission result: " + requestCode);
                base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

                return;
            }

            if (grantResults.Length != 0 && grantResults[0] == Permission.Granted)
            {
                Log.Debug(TAG, "Camera permission granted - initialize the camera source");
                // we have permission, so create the camerasource
                createCameraSource();
                return;
            }

            Log.Error(TAG, "Permission not granted: results len = " + grantResults.Length +
                    " Result code = " + (grantResults.Length > 0 ? grantResults[0].ToString() : "(empty)"));


            var builder = new Android.Support.V7.App.AlertDialog.Builder(this);
            builder.SetTitle("GoogleEyes")
                    .SetMessage(Resource.String.no_camera_permission)
                    .SetPositiveButton(Resource.String.ok, (o, e) => Finish())
                    .Show();

        }

        public Tracker Create(Java.Lang.Object item)
        {
            return new GooglyFaceTracker(mGraphicOverlay);
        }


    }

    class GraphicFaceTracker : Tracker, CameraSource.IPictureCallback
    {
        private GraphicOverlay mOverlay;
        private FaceGraphic mFaceGraphic;
        private CameraSource mCameraSource = null;
        private bool isProcessing = false;

        public GraphicFaceTracker(GraphicOverlay overlay, CameraSource cameraSource = null)
        {
            mOverlay = overlay;
            mFaceGraphic = new FaceGraphic(overlay);
            mCameraSource = cameraSource;
        }

        public override void OnNewItem(int id, Java.Lang.Object item)
        {
            mFaceGraphic.SetId(id);
            if (mCameraSource != null && !isProcessing)
                mCameraSource.TakePicture(null, this);
        }

        public override void OnUpdate(Detector.Detections detections, Java.Lang.Object item)
        {
            var face = item as Face;
            mOverlay.Add(mFaceGraphic);
            mFaceGraphic.UpdateFace(face);

        }

        public override void OnMissing(Detector.Detections detections)
        {
            mOverlay.Remove(mFaceGraphic);

        }

        public override void OnDone()
        {
            mOverlay.Remove(mFaceGraphic);

        }

        public void OnPictureTaken(byte[] data)
        {
            Task.Run(async () =>
            {
                try
                {
                    isProcessing = true;

                    System.Console.WriteLine("face detected: ");
                    

                }

                finally
                {
                    isProcessing = false;


                }

            });
        }
    }


}