using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fractural.SceneManagement.Transitions
{
    public class FadeTransition : SceneTransition
    {
        [Export]
        public float TransitionInDuration { get; set; }
        [Export]
        public Curve TransitionInCurve { get; set; }

        [Export]
        public float TransitionOutDuration { get; set; }
        [Export]
        public Curve TransitionOutCurve { get; set; }

        public enum State
        {
            Idle,
            TransitionIn,
            TransitionOut,
        }
        public State TransitionState { get; set; } = State.Idle;

        [Export]
        private NodePath _colorRectPath;
        private ColorRect _colorRect;

        private float _initialAlpha;
        private float _targetAlpha;
        private float _timer;

        public override void _Ready()
        {
            _colorRect = GetNode<ColorRect>(_colorRectPath);
            _colorRect.Color = new Color(_colorRect.Color, 0);
        }

        public override void _Process(float delta)
        {
            if (TransitionState == State.TransitionIn)
            {
                var originalColor = _colorRect.Color;
                originalColor.a = Mathf.Lerp(_initialAlpha, _targetAlpha, TransitionInCurve.Interpolate(_timer / TransitionInDuration));
                _colorRect.Color = originalColor;
                if (_timer < TransitionInDuration)
                    _timer += delta;
                else
                {
                    TransitionState = State.Idle;
                    EmitSignal(nameof(OnTransitionedIn));
                }
            }
            else if (TransitionState == State.TransitionOut)
            {
                var originalColor = _colorRect.Color;
                originalColor.a = Mathf.Lerp(_initialAlpha, _targetAlpha, TransitionInCurve.Interpolate(_timer / TransitionOutDuration));
                _colorRect.Color = originalColor;
                if (_timer < TransitionOutDuration)
                    _timer += delta;
                else
                {
                    TransitionState = State.Idle;
                    EmitSignal(nameof(OnTransitionedOut));
                }
            }
        }

        public override void TransitionIn()
        {
            _timer = 0;
            _initialAlpha = _colorRect.Color.a;
            _targetAlpha = 1;
            TransitionState = State.TransitionIn;
        }

        public override void TransitionOut()
        {
            _timer = 0;
            _initialAlpha = _colorRect.Color.a;
            _targetAlpha = 0;
            TransitionState = State.TransitionOut;
        }
    }
}
