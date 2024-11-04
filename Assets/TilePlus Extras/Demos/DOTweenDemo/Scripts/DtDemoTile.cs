#if TPT_DOTWEEN || DOTWEEN

using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Tilemaps;
using TilePlus;
// ReSharper disable AnnotateNotNullParameter

namespace TilePlusDemo
{
    /// <summary>
    ///  This is a simple demo tile showing how to use the TpDOTweenPlugin.
    /// </summary>
    [CreateAssetMenu(fileName = "DtDemoTile.asset", menuName = "TilePlus/Demo/Create DtDemoTile tile", order = 1000)]
    public class DtDemoTile : TilePlusBase
    {
        
        /// <summary>
        /// Set this tile to use two tweens or one sequence.
        /// </summary>
        public enum Mode 
        {
            /// <summary>
            /// Use tweens
            /// </summary>
            Tween,
            /// <summary>
            /// Use a sequence.
            /// </summary>
            Sequence
        }

        /// <summary>
        /// What this tile should tween
        /// </summary>
        public enum Operation
        {
            /// <summary>
            /// Tween scale
            /// </summary>
            Scale,
            /// <summary>
            /// Tween position
            /// </summary>
            Position,
            /// <summary>
            /// Tween rotation
            /// </summary>
            Rotation,
            /// <summary>
            /// Tween color
            /// </summary>
            Color
        }

        #region publicFields
        /// <summary>
        /// The type of operation
        /// </summary>
        [TptShowEnum()][Tooltip("Scale,Translate,Color change, Rotation")]
        public Operation m_Operation = Operation.Scale;
        /// <summary>
        /// Scaling?
        /// </summary>
        public bool      OperationIsScaling  => m_Operation == Operation.Scale;
        /// <summary>
        /// Translation?
        /// </summary>
        public bool      OperationIsPosition => m_Operation == Operation.Position;
        /// <summary>
        /// Color change?
        /// </summary>
        public bool      OperationIsColor    => m_Operation == Operation.Color;
        /// <summary>
        /// Rotation
        /// </summary>
        public bool      OperationIsRotation => m_Operation == Operation.Rotation;
        
        
        /// <summary>
        /// The initial size for the sprite
        /// </summary>
        [TptShowField(0f,0f,SpaceMode.None,ShowMode.Property,"OperationIsScaling")]
        [Tooltip("Start size for the sprite when tweening scale")]
        public Vector3 m_StartSize = Vector3.one;
        /// <summary>
        /// The end size for the sprite
        /// </summary>
        [TptShowField(0f,0f,SpaceMode.None,ShowMode.Property,"OperationIsScaling")] 
        [Tooltip("End size for the sprite when tweening scale")]
        public Vector3 m_EndSize = new Vector3(2, 2, 1);

        /// <summary>
        /// Starting color
        /// </summary>
        [Tooltip("Start color for the sprite when tweening color")]
        [TptShowField(0f,0f,SpaceMode.None,ShowMode.Property,"OperationIsColor")]
        public Color m_StartColor = Color.white;

        /// <summary>
        /// Ending color
        /// </summary>
        [Tooltip("End color for the sprite when tweening color")]
        [TptShowField(0f,0f,SpaceMode.None,ShowMode.Property,"OperationIsColor")]
        public Color m_EndColor = Color.white;

        /// <summary>
        /// Starting rotation
        /// </summary>
        [Tooltip("Start Rotation for the sprite when tweening Rotation")]
        [TptShowField(0f,0f,SpaceMode.None,ShowMode.Property,"OperationIsRotation")]
        public Vector3 m_StartRotation = Vector3.zero;

        /// <summary>
        /// Ending rotation
        /// </summary>
        [Tooltip("End Rotation for the sprite when tweening Rotation")]
        [TptShowField(0f,0f,SpaceMode.None,ShowMode.Property,"OperationIsRotation")]
        public Vector3 m_EndRotation = Vector3.zero;

        /// <summary>
        /// Starting position
        /// </summary>
        [Tooltip("Start position for the sprite when tweening position (translation)")]
        [TptShowField(0f,0f,SpaceMode.None,ShowMode.Property,"OperationIsPosition")]
        public Vector3 m_StartPosition = Vector3.zero;

        /// <summary>
        /// Ending position
        /// </summary>
        [Tooltip("End position for the sprite when tweening position (translation)")]
        [TptShowField(0f,0f,SpaceMode.None,ShowMode.Property,"OperationIsPosition")]
        public Vector3 m_EndPosition = Vector3.zero;
        
        /// <summary>
        /// Duration of the tween or sequence
        /// </summary>
        [TptShowField()] [Tooltip("Duration of the tween/sequence")]
        public float m_Duration = 1f;
        
        /// <summary>
        /// Select the mode (tweens or a sequence)
        /// </summary>
        [TptShowEnum(SpaceMode.None,ShowMode.Property,"OperationIsScaling")] 
        [Tooltip("Select tweens or sequence of tweens")]
        public Mode m_Mode;

        /// <summary>
        /// Easing type
        /// </summary>
        [TptShowEnum()]
        [Tooltip("Select easing type")]
        public Ease m_EaseType = Ease.Linear;

        #endregion
        
        #region privateFields
        
        
        
        //state for scaling method w/ two tweens
        [NonSerialized]
        private bool isEnlarged;
        
        private readonly DOTweenAdapter dtAdapter = new();
        
        
        
        #endregion
        
        #region code

        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            //ALWAYS do this first and return if false!!
            if (!base.StartUp(position, tilemap, go))
                return false;

            if (!Application.isPlaying) //if not in Play mode then don't do anything else.
                return true;

            isEnlarged   = false;
            
            //Set the initial values
            m_ParentTilemap!.SetTileFlags( m_TileGridPosition,TileFlags.None); //ensure that transform and color can be changed.
            flags = TileFlags.None;
            
            //ensure transform components are set to 'neutral'
            TileUtil.SetTransform(m_ParentTilemap, m_TileGridPosition, Vector3.zero, Vector3.zero, Vector3.one);
            transform = m_ParentTilemap.GetTransformMatrix(m_TileGridPosition);
            
            //ensure starting color is set, ensure alpha isn't 0.
            if (m_StartColor.a == 0)
                m_StartColor.a = 1f; //to avoid invisible sprites
            color = m_StartColor;
            m_ParentTilemap.SetColor(m_TileGridPosition,m_StartColor);

            //Initialize DOTween as usual
            DOTween.Init();
            
            TpLib.DelayedCallback(this,StartEffect,"DtDemoTile:StartEffect",10,true);
           
            return true;
        }

        /// <summary>
        /// Begin one of the various tweening demos
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void StartEffect()
        {
            if (dtAdapter == null)
            {
                Debug.LogError("Dotween Adapter missing...");
                return;
            }


            //choose which operation to perform
            switch (m_Operation)
            {
                case Operation.Scale when m_Mode == Mode.Tween:
                    ScalingTweens();
                    break;
                case Operation.Scale:
                    ScalingSequence();
                    break;
                case Operation.Color:
                    ColorSequence();
                    break;
                case Operation.Position:
                    PositionSequence();
                    break;
                case Operation.Rotation:
                    RotationSequence();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        
        /// <summary>
        /// OnDisable event handler
        /// </summary>
        protected void OnDisable()
        {
            dtAdapter.OnDisableHandler();
        }

        /// <summary>
        /// Reset state override
        /// </summary>
        /// <param name="op"></param>
        public override void ResetState(TileResetOperation op)
        {
            base.ResetState(op);
            isEnlarged = false;
            dtAdapter?.OnDisableHandler();
        }


        private void RotationSequence()
        {
            //create a sequence
            var sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(() => RotationProp, x => RotationProp = x, m_EndRotation, m_Duration).SetEase(m_EaseType));
            sequence.Append(DOTween.To(() => RotationProp, x => RotationProp = x, m_StartRotation, m_Duration).SetEase(m_EaseType));
            sequence.SetLoops(-1, LoopType.Restart);
            dtAdapter.AddSequence(sequence); //add sequence to the per-tile controller
            sequence.Play();
        }
        
        private Vector3 RotationProp
        {
            get => TileUtil.GetTransformRotation(m_ParentTilemap!, m_TileGridPosition);
            set => TileUtil.SetTransform(m_ParentTilemap!, m_TileGridPosition, Vector3.zero, value, Vector3.one);
        }

        private void PositionSequence()
        {
            //create a sequence
            var sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(() => PositionProp, x => PositionProp = x, m_EndPosition, m_Duration).SetEase(m_EaseType));
            sequence.Append(DOTween.To(() => PositionProp, x => PositionProp = x, m_StartPosition, m_Duration).SetEase(m_EaseType));
            sequence.SetLoops(-1, LoopType.Restart);

            dtAdapter.AddSequence(sequence); //add sequence to the per-tile controller
            sequence.Play();
        }

        private Vector3 PositionProp
        {
            get => TileUtil.GetTransformPosition(m_ParentTilemap!, m_TileGridPosition);
            set => TileUtil.SetTransform(m_ParentTilemap!, m_TileGridPosition, value, Vector3.zero, Vector3.one);
        }

        
        private void ColorSequence()
        {
            //create a sequence
            var sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(() => ColorProp, x => ColorProp = x, m_EndColor, m_Duration).SetEase(m_EaseType));
            sequence.Append(DOTween.To(() => ColorProp, x => ColorProp = x, m_StartColor, m_Duration).SetEase(m_EaseType));
            sequence.SetLoops(-1, LoopType.Restart);
            dtAdapter.AddSequence(sequence); //add sequence to the per-tile controller
            sequence.Play();
        }

        private Color ColorProp
        {
            get => m_ParentTilemap!.GetColor(m_TileGridPosition);
            set => m_ParentTilemap!.SetColor(m_TileGridPosition, value);
        }

        /// <summary>
        /// this is what executes when operation is scaling && mode is set to tween
        /// </summary>
        private void ScalingTweens()
        {
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if(isEnlarged)
                dtAdapter.AddTween(DOTween.To(() => SizeProp, x => SizeProp = x, m_StartSize, m_Duration).SetEase(m_EaseType).SetLoops(-1,LoopType.Restart));
            else
                dtAdapter.AddTween(DOTween.To(() => SizeProp, x => SizeProp = x, m_EndSize, m_Duration).SetEase(m_EaseType).SetLoops(-1, LoopType.Restart));            
        }


        /// <summary>
        /// this is what executes when operation is scaling & mode is set to sequence
        /// </summary>
        private void ScalingSequence()
        {
            //create a sequence
            var sequence = DOTween.Sequence();
            sequence.Append(DOTween.To(() => SizeProp, x => SizeProp = x, m_EndSize, m_Duration).SetEase(m_EaseType));
            sequence.Append(DOTween.To(() => SizeProp, x => SizeProp = x, m_StartSize, m_Duration).SetEase(m_EaseType));
            sequence.SetLoops(-1, LoopType.Restart);
            dtAdapter.AddSequence(sequence); //add sequence to the per-tile controller
            sequence.Play();
        }


        /// <summary>
        /// The 'getter/setter' as required by DOTween.
        /// </summary>
        private Vector3 SizeProp
        {
            get => TileUtil.GetTransformScale(m_ParentTilemap!, m_TileGridPosition);
            set => TileUtil.SetTransform(m_ParentTilemap!, m_TileGridPosition, Vector3.zero, Vector3.zero, value);
        }
        #endregion

        #region editor
        #if UNITY_EDITOR
        /// <inheritdoc />
        public override bool InternalLockColor => true;
        /// <inheritdoc />
        public override bool InternalLockTransform => true;
        #endif
        #endregion






    }
}
#endif
