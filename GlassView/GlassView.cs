using Android.Annotation;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Renderscripts;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using Object = Java.Lang.Object;

namespace GlassViewDroid
{
    public class GlassView : RelativeLayout
    {
        private const int DefaultDownSampling = 3;
        private const float DefaultBlurRadius = 5f;
        private const float MaxBlurRadius = 25f;

        private RenderScript _renderScript;
        private ScriptIntrinsicBlur _blur;
        private Allocation _in;
        private Allocation _out;

        private Bitmap _origBitmap;
        private Bitmap _blurredBitmap;

        private Canvas _blurCanvas;
        private Rect _destRect;

        private float _scaleFactor;

        private bool _parentViewDrawn;

        public GlassView(Context context)
            : base(context)
        {
            Initialize();
        }

        public GlassView(Context context, IAttributeSet attrs)
            : this(context, attrs, 0)
        { }

        public GlassView(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {
            Initialize(attrs);
        }

        protected GlassView(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            Initialize();
        }

        private void Initialize()
        {
            SetWillNotDraw(false);
        }

        private void Initialize(IAttributeSet attrs)
        {
            Initialize();

            var typedArray = Context.ObtainStyledAttributes(attrs, Resource.Styleable.GlassView);
            _downSampling = typedArray.GetInt(Resource.Styleable.GlassView_DownSampling, DefaultDownSampling);
            BlurRadius = typedArray.GetFloat(Resource.Styleable.GlassView_BlurRadius, DefaultBlurRadius);
            typedArray.Recycle();
        }

        private void PrepareBitmaps()
        {
            if(MeasuredHeight > 0)
            {
                CleanUpBitmaps();
                _scaleFactor = 1f / _downSampling;
				_origBitmap = Bitmap.CreateBitmap((int)(MeasuredWidth * _scaleFactor), (int)(MeasuredHeight * _scaleFactor), Bitmap.Config.Argb8888);
                _blurredBitmap = _origBitmap.Copy(_origBitmap.GetConfig(), true);
                _blurCanvas = new Canvas(_origBitmap);
            }
        }

        private GlassGlobalListener _listener;
        private GlassGlobalListener Listener
        {
            get
            {
                if (_listener == null)
                {
                    _listener = new GlassGlobalListener(this);
                }

                return _listener;
            }
        }

		private int _downSampling = DefaultDownSampling;
		public int DownSampling 
		{
			get 
			{
				return _downSampling;
			}
			set 
			{
				_downSampling = value;
			}
		}

        private float _blurRadius = DefaultBlurRadius;
        public float BlurRadius
        {
            get
            {
                return _blurRadius;
            }
            set
            {
                if (0f < value && value <= MaxBlurRadius)
                {
                    _blurRadius = value;
                    Invalidate();
                }
            }
        }

        private void ApplyBlur()
        {
            _in = Allocation.CreateFromBitmap(_renderScript, _origBitmap);
            _out = Allocation.CreateTyped(_renderScript, _in.Type);
            _blur.SetRadius(_blurRadius);
            _blur.SetInput(_in);
            _blur.ForEach(_out);
            _out.CopyTo(_blurredBitmap);
        }

        private void DrawParentToBitmap(View parent)
        {
            _blurCanvas.Save();
            if (_downSampling > 1)
            {
                _blurCanvas.Translate(-Left * _scaleFactor, -Top * _scaleFactor);
                _blurCanvas.Scale(_scaleFactor, _scaleFactor);
            }
            else
            {
                _blurCanvas.Translate(-Left, -Top);
            }

            parent.Draw(_blurCanvas);
            _blurCanvas.Restore();
        }

        private void CleanUpBitmaps()
        {
            if (_origBitmap != null)
            {
                _origBitmap.Recycle();
                _origBitmap = null;
            }
            if (_blurredBitmap != null)
            {
                _blurredBitmap.Recycle();
                _blurredBitmap = null;
            }
        }

        private static bool IsPostHoneycomb
        {
            get
            {
                return Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb;
            }
        }

        [TargetApi(Value = (int)BuildVersionCodes.Honeycomb)]
        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            _renderScript = RenderScript.Create(Context);
            _blur = ScriptIntrinsicBlur.Create(_renderScript, Element.U8_4(_renderScript));

            if (IsPostHoneycomb && IsHardwareAccelerated)
            {
                ViewTreeObserver.AddOnGlobalLayoutListener(Listener);
                ViewTreeObserver.AddOnScrollChangedListener(Listener);
            }
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();

            if (_blur != null)
            {
                _blur.Destroy();
                _blur = null;
            }

            if (_renderScript != null)
            {
                _renderScript.Destroy();
                _renderScript = null;
            }

            CleanUpBitmaps();


            if (_listener != null)
            {
                var vto = ViewTreeObserver;

                if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBean)
                {
                    vto.RemoveOnGlobalLayoutListener(_listener);
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    vto.RemoveGlobalOnLayoutListener(_listener);
#pragma warning restore CS0618 // Type or member is obsolete
                }

                vto.RemoveOnScrollChangedListener(_listener);
            }
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            if(MeasuredHeight > 0)
            {
                PrepareBitmaps();
                _destRect = new Rect(0, 0, MeasuredWidth, MeasuredHeight);
            }
        }

        public override void Draw(Canvas canvas)
        {
            var parent = Parent as View;

            // prevent draw() from being recursively called
            if (!_parentViewDrawn)
            {
                _parentViewDrawn = true;

                DrawParentToBitmap(parent);
                ApplyBlur();

                canvas.DrawBitmap(_blurredBitmap, null, _destRect, null);
                base.Draw(canvas);
                _parentViewDrawn = false;
            }
        }
        private class GlassGlobalListener : Object, ViewTreeObserver.IOnGlobalLayoutListener, ViewTreeObserver.IOnScrollChangedListener
        {
            private readonly GlassView _glassView;

            public GlassGlobalListener(GlassView glassView)
            {
                _glassView = glassView;
            }

            public void OnGlobalLayout()
            {
                _glassView.Invalidate();
            }

            public void OnScrollChanged()
            {
                _glassView.Invalidate();
            }
        }
    }
}