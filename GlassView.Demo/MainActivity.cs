using Android.App;
using Android.Views;
using Android.Widget;
using Android.OS;
using Java.Lang;
using GlassViewDroid;

namespace GlassViewDroid.Demo
{
    [Activity(Label = "GlassView.Demo", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    { 
		private GlassView TopGlassView { get; set;}
		private GlassView BottomGlassView { get; set;}

        private ImageView _bgImg;
        private SeekBar _seekBar;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MainActivity);

			TopGlassView = FindViewById<GlassView>(Resource.Id.TopGlassView);
			BottomGlassView = FindViewById<GlassView>(Resource.Id.BottomGlassView);

            _bgImg = FindViewById<ImageView>(Resource.Id.BgImg);
            _seekBar = FindViewById<SeekBar>(Resource.Id.SeekBar);

            _seekBar.SetOnSeekBarChangeListener(new SeekBarChangeListener(this));
        }

        private class SeekBarChangeListener : Object, SeekBar.IOnSeekBarChangeListener
        {
            private readonly MainActivity _activity;

            public SeekBarChangeListener(MainActivity activity)
            {
                _activity = activity;
            }

            public void OnProgressChanged(SeekBar seekBar, int progress, bool fromUser)
            {
                // allow blur radius is 0 < r <= 25
                if (progress > 0)
                {
                    _activity.TopGlassView.BlurRadius = progress;
                    _activity.BottomGlassView.BlurRadius = progress;
                }
            }

            public void OnStartTrackingTouch(SeekBar seekBar)
            {
            }

            public void OnStopTrackingTouch(SeekBar seekBar)
            {
            }
        }
    }
}

