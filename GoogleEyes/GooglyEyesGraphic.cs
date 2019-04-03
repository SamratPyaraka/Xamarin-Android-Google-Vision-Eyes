using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using GoogleEyes.CustomControls;

namespace GoogleEyes
{
    public class GooglyEyesGraphic : GraphicOverlay.Graphic
    {
        private static readonly float EYE_RADIUS_PROPORTION = 0.45f;
        private static readonly float IRIS_RADIUS_PROPORTION = EYE_RADIUS_PROPORTION / 2.0f;

        private Paint mEyeWhitesPaint;
        private Paint mEyeIrisPaint;
        private Paint mEyeOutlinePaint;
        private Paint mEyeLidPaint;

        // Keep independent physics state for each eye.
        private EyePhysics mLeftPhysics = new EyePhysics();
        private EyePhysics mRightPhysics = new EyePhysics();

        private volatile PointF mLeftPosition;
        private volatile bool mLeftOpen;

        private volatile PointF mRightPosition;
        private volatile bool mRightOpen;
        
        

        private int mFaceId;

        private static Color[] COLOR_CHOICES = {
            Color.Blue,
            Color.Cyan,
            Color.Green,
            Color.Magenta,
            Color.Red,
            Color.White,
            Color.Yellow
        };

        public void SetId(int id)
        {
            mFaceId = id;
        }

        public GooglyEyesGraphic(GraphicOverlay overlay):base(overlay)
        {
            //super(overlay);

            mEyeWhitesPaint = new Paint();
            mEyeWhitesPaint.Color = Color.White;
            mEyeWhitesPaint.SetStyle(Paint.Style.Fill);

            mEyeLidPaint = new Paint();
            mEyeLidPaint.Color=Color.Yellow;
            mEyeLidPaint.SetStyle(Paint.Style.Fill);

            mEyeIrisPaint = new Paint();
            mEyeIrisPaint.Color=Color.Black;
            mEyeIrisPaint.SetStyle(Paint.Style.Fill);

            mEyeOutlinePaint = new Paint();
            mEyeOutlinePaint.Color = Color.Black;
            mEyeOutlinePaint.SetStyle(Paint.Style.Stroke);
            //mEyeOutlinePaint.setStrokeWidth(5);
            mEyeOutlinePaint.StrokeWidth = 5.0f;
            
        }

        

        public void updateEyes(PointF leftPosition, bool leftOpen,
                    PointF rightPosition, bool rightOpen)
        {
            mLeftPosition = leftPosition;
            mLeftOpen = leftOpen;

            mRightPosition = rightPosition;
            mRightOpen = rightOpen;

            PostInvalidate();
        }

        public override void Draw(Canvas canvas)
        {
            PointF detectLeftPosition = mLeftPosition;
            PointF detectRightPosition = mRightPosition;
            if ((detectLeftPosition == null) || (detectRightPosition == null))
            {
                return;
            }

            PointF leftPosition =
                    new PointF(TranslateX(detectLeftPosition.X), TranslateY(detectLeftPosition.Y));
            PointF rightPosition =
                    new PointF(TranslateX(detectRightPosition.X), TranslateY(detectRightPosition.Y));

            // Use the inter-eye distance to set the size of the eyes.
            float distance = (float)Math.Sqrt(
                    Math.Pow(rightPosition.X - leftPosition.X, 2) +
                    Math.Pow(rightPosition.Y - leftPosition.Y, 2));
            float eyeRadius = EYE_RADIUS_PROPORTION * distance;
            float irisRadius = IRIS_RADIUS_PROPORTION * distance;

            // Advance the current left iris position, and draw left eye.
            PointF leftIrisPosition =
                    mLeftPhysics.nextIrisPosition(leftPosition, eyeRadius, irisRadius);
            drawEye(canvas, leftPosition, eyeRadius, leftIrisPosition, irisRadius, mLeftOpen);

            // Advance the current right iris position, and draw right eye.
            PointF rightIrisPosition =
                    mRightPhysics.nextIrisPosition(rightPosition, eyeRadius, irisRadius);
            drawEye(canvas, rightPosition, eyeRadius, rightIrisPosition, irisRadius, mRightOpen);
        }

         /**
            * Draws the eye, either closed or open with the iris in the current position.
            */

        private void drawEye(Canvas canvas, PointF eyePosition, float eyeRadius,
                         PointF irisPosition, float irisRadius, bool isOpen)
        {
            if (isOpen)
            {
                canvas.DrawCircle(eyePosition.X, eyePosition.Y, eyeRadius, mEyeWhitesPaint);
                canvas.DrawCircle(irisPosition.X, irisPosition.Y, irisRadius, mEyeIrisPaint);
            }
            else
            {
                canvas.DrawCircle(eyePosition.X, eyePosition.Y, eyeRadius, mEyeLidPaint);
                float y = eyePosition.Y;
                float start = eyePosition.X - eyeRadius;
                float end = eyePosition.X + eyeRadius;

                
                canvas.DrawLine(start, y, end, y, mEyeOutlinePaint);
            }
            canvas.DrawCircle(eyePosition.X, eyePosition.Y, eyeRadius, mEyeOutlinePaint);
        }

       
    }
}