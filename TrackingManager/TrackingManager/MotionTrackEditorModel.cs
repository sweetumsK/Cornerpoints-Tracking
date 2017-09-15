using System;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading;
using System.Runtime.InteropServices;

using Avid.AppFramework;
using Avid.AppFramework.WPF;
using Avid.NGStudio.Api;
using Avid.NGStudio.JohanssonTools;
using Avid.PinnacleServices;
using Avid.PinnacleServices.AssetDb;
using Avid.NGStudio.EditorComponents.Util;
using Avid.NGStudio.EditorComponents.ClipEditorComponents;
using MediaEditor;
using Avid.PinnacleServices.Commands.Sequence;
using Avid.NGStudio.EditorComponents.MovieEditorComponents;

namespace MotionTrackEditor
{
    class TimelinePanelItem
    {
        public TimelinePanelItem(double startTime, double duration)
        {
            _startTime = startTime;
            _duration = duration;
        }

        private double _startTime = 0;
        public double StartTime
        {
            get
            {
                return _startTime;
            }
            set
            {
                _startTime = value;
            }
        }

        private double _duration = 0;
        public double Duration
        {
            get
            {
                return _duration;
            }
            set
            {
                _duration = value;
            }
        }
    }
    class MotionTrackEditorModel : EditorModel, IMotionTrackEditorModel, IPublicInterface
    {
        #region IPublicInterface

        public string Description
        {
            get { return "MotionTrack Editor Model"; }
        }

        #endregion

        #region Model

        public override void Initialize(MVCInfo info)
        {
            using (new GBMess(DebugUser.Perf.Misc.OpenEditor, "MotionTrackEditorModel::Initialize"))
            {
                base.Initialize(info);

                if (_editorBBModel != null)
                    _editorBBModel.Initialize(this);

                _fx_maskobjects.IsMotionTrack = true;
                _fx_followobjects.IsMotionTrack = true;
            }

            if (MaskVisibility == Visibility.Collapsed)
            {
                MotionTabSelectIndex = 1;
            }
        }

        public override bool Shutdown()
        {
            TAUManager.UnregisterObject("MotionTrackEditorModel");

            if (_compactLibraryModel != null)
            {
                _compactLibraryModel.LibraryModel.BrowserItemExecute -= OnBrowserItemExecute;
            }

            MemDebug.ShouldCollect(this, typeof(MotionTrackEditorModel).Name, "MotionTrackEditorModel");

            if (_previewModel != null)
                _previewModel.PropertyChanged -= OnPreviewModelChanged;

            base.Shutdown();
            CloseDocument();
            return true;
        }

        private ISequence GetRootSequence(IDocument document)
        {
            IObjectSnapshot snap = document.GetObject(document.RootObjectId);
            if (snap.Is<IDVDProject>())
            {
                IDVDProject prj = snap.As<IDVDProject>();
                uint count = prj.BucketCount;
                if (count > 0)
                {
                    // ToDo: for Johansson ver1 one we are only dealing with single bucket
                    return prj.GetBucket(0).GetSequence();
                }
            }
            else if (snap.Is<ISequence>())
            {
                return snap.As<ISequence>();
            }
            return null;
        }

        public override object objData
        {
            set
            {
                using (new GBMess(DebugUser.Perf.Misc.OpenEditor, "MotionTrackEditorModel::objData::set"))
                {
                    base.objData = value;
                    if (objData == null)
                        return;

                    _parentTrack = null;
                    _parentSequence = null;
                    if (objData is ObjectsInDocument)
                    {
                        ObjectsInDocument objInDocument = objData as ObjectsInDocument;
                        InitialViewTime = objInDocument.ViewTime;
                        IDocumentManager docManager = PublicInterface.Get<IDocumentManager>();
                        IDocument document = docManager.GetDocument(objInDocument.Document.DocumentId);
                        _rootSequence = GetRootSequence(document);
                        if (document != null)
                        {
                            // find parent track and sequence
                            for (int i = 1; i < objInDocument.AllObjects.Length; i++)
                            {
                                ITrack iTrack = document.GetObject(objInDocument.AllObjects[i]).As<ITrack>();
                                if (iTrack != null)
                                    _parentTrack = iTrack;
                                ISequence iSequence = document.GetObject(objInDocument.AllObjects[i]).As<ISequence>();
                                if (iSequence != null)
                                    _parentSequence = iSequence;
                            }
                            if (_parentSequence == null)
                            {
                                ISequence iSequence = _rootSequence;
                                if (iSequence != null)
                                    _parentSequence = iSequence;
                            }

                            if (_parentSequence != null)
                            {
                                _parentSequence.GetTrackAndIndex(objInDocument.ObjectID, out _trackIndex, out _clipIndex);
                            }

                            SetDocument(document, objInDocument.ObjectID);

                            if (_parentSequence != null)
                            {
                                ISource iSource = _parentSequence.As<ISource>();
                                VideoFormat VF;
                                VF.FrameLength = 1 / 30.0;
                                VF.PixelX = 0;
                                VF.PixelY = 0;
                                VF.PixelAspect = 1;
                                VF.InterlacedType = InterlacedType.Progressive;
                                VF.StereoscopicType = StereoscopicType.None;

                                VideoSourceInfo info;
                                if (iSource.GetVideoSourceInfo(new MTime(0), out info))
                                {
                                    VF = info.VideoFormat;
                                }
                                VideoFormat = VF;

                                MTime _overlapin = _parentTrack.GetOverlapIn(_clipIndex);
                                MTime _overlapout = _parentTrack.GetOverlapOut(_clipIndex);

                                TlMarkIn = _parentTrack.GetMarkIn(_clipIndex) - _overlapin;
                                TlMarkOut = _parentTrack.GetMarkOut(_clipIndex) + _overlapout;

                                MTime rawRecIn = _parentTrack.GetRecIn(_clipIndex);
                                MTime rawRecOut = _parentTrack.GetRecIn(_clipIndex) + _parentTrack.GetDuration(_clipIndex);

                                TlRecIn = rawRecIn - _overlapin;
                                TlRecOut = rawRecOut + _overlapout;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region constructor

        public MotionTrackEditorModel()
        {
            using (new GBMess(DebugUser.Perf.Misc.OpenEditor, "MotionTrackEditorModel()"))
            {
                PropertyChanged += OnPropertyChanged;

                _uiDispatcher = Dispatcher.CurrentDispatcher;

                _compactLibraryModel = ClassTypeImplementingInterface.CreateInstance<IRichLibraryModel>();
                if (_compactLibraryModel != null)
                {
                    _compactLibraryModel.IsFullsized = false;
                    _compactLibraryModel.LibraryModel.BrowserItemExecute += OnBrowserItemExecute;
                    _compactLibraryModel.OpenEditorOnExecute = false;
                }
                AddNestedModel(_compactLibraryModel);

                _panZoomModel = ClassTypeImplementingInterface.CreateInstance<IPanZoomModel>();
                AddNestedModel(_panZoomModel);

                _previewModel = ClassTypeImplementingInterface.CreateInstance<IPreviewMediaPlayerModel>();
                if (_previewModel != null)
                {
                    _previewModel.M_Player.PlaybackQualityMode = PlaybackQuality.PQ_Editing;
                    _previewModel.PropertyChanged += OnPreviewModelChanged;
                }
                AddNestedModel(_previewModel);

                _editorBBModel = ClassTypeImplementingInterface.CreateInstance<IEditorBottomBarModel>();
                _editorBBModel.PanZoomModel = _panZoomModel;
                if (_editorBBModel.PanZoomModel != null)
                    _editorBBModel.PanZoomModel.PanZoomState = PanZoomStates.ZoomMenuVisible;
                _editorBBModel.LibraryModel = _compactLibraryModel;
                _editorBBModel.Elements = EditorBottomBarElements.MotionTrackEditor;
                _editorBBModel.CancelCommand = new StateModelCommand(CancelCommandExecute, null, false);
                _editorBBModel.OkCommand = new StateModelCommand(OkCommandExecute, null, false);
                _editorBBModel.FullScreenCommand = new StateModelCommand(FullScreenCommandExecute, null, false);
                AddNestedModel(_editorBBModel);

                M_MaskObjectEffectEditor = ClassTypeImplementingInterface.CreateInstance<IEffectModel>() as EffectModel;
                M_MaskObjectEffectEditor.PropertySetted += OnEffectEditorPropertySetted;
                AddNestedModel(M_MaskObjectEffectEditor as Model);

                M_FollowObjectEffectEditor = ClassTypeImplementingInterface.CreateInstance<IEffectModel>() as EffectModel;
                M_FollowObjectEffectEditor.PropertySetted += OnEffectEditorPropertySetted;
                AddNestedModel(M_FollowObjectEffectEditor as Model);

                TAUManager.RegisterObject("MotionTrackEditorModel", this);
            }
        }

        ~MotionTrackEditorModel()
        {
            PropertyChanged -= OnPropertyChanged;
        }

        private void CancelCommandExecute(object parameter)
        {
            CancelAndClose();
        }
        private void OkCommandExecute(object parameter)
        {
            SaveAndClose(false);
        }

        private void FullScreenCommandExecute(object parameter)
        {
            _previewModel.M_Player.FullScreenCommand.Execute(parameter);
        }

        Dispatcher _uiDispatcher;

        #endregion

        #region Sub-Models

        IRichLibraryModel _compactLibraryModel;
        public IRichLibraryModel CompactLibraryModel
        {
            get { return _compactLibraryModel; }
        }

        private void OnBrowserItemExecute(object sender, BrowserItemExecuteEventArgs e)
        {
            // never open an editor on an item in scenes view
            if (_compactLibraryModel.LibraryModel.IsScenesViewActive)
                return;

            IAssetTypeRegistry iATR = PublicInterface.Get<IAssetTypeRegistry>();
            if (iATR != null)
            {
                for (int i = e.ExecutedDbObjects.Count - 1; i >= 0; i--)
                {
                    if (iATR.IsDocumentAsset (e.ExecutedDbObjects[i].Record.AssetType))
                        e.ExecutedDbObjects.RemoveAt (i);
                }
            }
            if (e.ExecutedDbObjects.Count == 0)
                return;

            IEditorModelManager em = PublicInterface.Get<IEditorModelManager>();
            if (null != em)
            {
                IEditorModel editor = em.GetEditorModel(e.ExecutedDbObjects, null, null);
                if (editor == null)
                    return; // no editor for this item found

                //open new
                IModel contextNavigator = _compactLibraryModel.CreateContextNavigator();

                editor.SetContextNavigationModel(contextNavigator, true);
                if (!editor.IsInitialized)
                {
                    editor.Initialize(new MVCInfo());
                }

                AddNestedEditorModel(editor);
            }
        }
        
        IPanZoomModel _panZoomModel;
        public IPanZoomModel PanZoomModel
        {
            get { return _panZoomModel; }
        }

        IPreviewMediaPlayerModel _previewModel;
        public IPreviewMediaPlayerModel PreviewModel
        {
            get { return _previewModel ;}
        }
        private MTime InitialViewTime = MTime.Unassigned;

        private IEditorBottomBarModel _editorBBModel;
        public IEditorBottomBarModel EditorBottomBarModel
        {
            get { return _editorBBModel; }
        }

        public void RequireRebuildMotionUI()
        {
            RebuildMotionUI();
        }

        System.Timers.Timer _motionUITimer = null;
        Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        protected void RebuildMotionUI()
        {
            if (_motionUITimer == null)
            {
                _motionUITimer = new System.Timers.Timer(1000);
                _motionUITimer.Elapsed += RefreshDrawingItem;
                _motionUITimer.AutoReset = false;
                _motionUITimer.Start();
            }
            else
            {
                _motionUITimer.Stop();
                _motionUITimer.Start();
            }

            MotionPath = null;
        }

        void RefreshDrawingItem(object sender, System.Timers.ElapsedEventArgs e)
        {
            _dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate
            {
                RefreshDrawingItem();
            }));
        }

        private void OnPreviewModelChanged (object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "OriginalSize")
                _panZoomModel.OriginalContentSize = _previewModel.OriginalSize;
            else if (e.PropertyName == "StartPos" || e.PropertyName == "EndPos")
            {
                _PreviewDuration = _previewModel.EndPos - _previewModel.StartPos;
                UpdateSectionProperties ();
            }
            else if (e.PropertyName == "Position")
            {
                RebuildMotionUI();
                RefreshInputListPosition();
            }
            else if (e.PropertyName == "IsPlaying")
            {
                RefreshInputListPosition();
                foreach (var obj in _inputPropList)
                {
                    obj.RefreshEnable = !PreviewModel.IsPlaying;
                }
            }
        }

        private void RefreshInputListPosition()
        {
            if (!this.PreviewModel.IsPlaying)
            {
                foreach (var obj in _inputPropList)
                {
                    if (obj.TrackObjectId == null)
                    {
                        continue;
                    }
                    IObjectSnapshot snap = Document.GetObject(obj.TrackObjectId);
                    ITrack track = snap == null ? null : snap.As<ITrack>();
                    if (track == null || track.GetClipCount() == 0)
                    {
                        continue;
                    }
                    obj.Position = PreviewModel.Position - track.GetClipTime(0, 0);
                }
            }
        }

        private double _PreviewDuration = 0;

        public double VisibleTimespan
        {
            get
            {
                return _VisibleTimespan;
            }
            set
            {
                if (_VisibleTimespan != value)
                {
                    _VisibleTimespan = value;
                    OnPropertyChanged("VisibleTimespan");
                }
            }
        }
        double _VisibleTimespan = 0;
        public double VisibleStartTime
        {
            get
            {
                return _VisibleStartTime;
            }
            set
            {
                if (_VisibleStartTime != value)
                {
                    _VisibleStartTime = value;
                    OnPropertyChanged("VisibleStartTime");
                }
            }
        }
        double _VisibleStartTime = 0;

        private void UpdatePreviewRange()
        {
            if (_parentTrack == null || _parentSequence == null || _previewModel == null || _previewModel.M_Player == null)
                return;
            
            UInt32 ClipTrackIndax = _parentTrack.GetClipIndex (_sourceID) ;
            MTime recin    = _parentTrack.GetRecIn(ClipTrackIndax);
            MTime duration = _parentTrack.GetDuration(ClipTrackIndax);
            MTime recout   = recin + duration;

            VisibleStartTime = recin;
            VisibleTimespan = duration;

            _previewModel.M_Player.StartPos = recin;
            _previewModel.M_Player.EndPos = recout;
            _previewModel.M_Player.Player.Start = recin;
            _previewModel.M_Player.Player.End   = recout;

            _previewModel.M_Transport.MarkIn  = recin;
            _previewModel.M_Transport.MarkOut = recout;

            if (InitialViewTime.IsUnassigned)
            {
                _previewModel.M_Player.Position = recin;
                _previewModel.M_Transport.Position = recin;
            }
        }

        #endregion

        #region source, parent track and parent sequence

        public VideoFormat VideoFormat { get; set; }

        ID _sourceID;
        ID SourceID
        {
            get
            {
                return _sourceID;
            }
            set
            {
                if (value != _sourceID)
                {
                    _sourceID = value ;
                    if (_document != null)
                    {
                        IObjectSnapshot objSnap = _document.GetObject(_sourceID);
                        if (objSnap != null)
                        {
                            if (objSnap.Is<IBaseClip>())
                            {
                                IBaseClip clip = objSnap.As<IBaseClip>();
                                SetEditorName(clip.Name);
                            }
                        }

                        InitInputList();
                    }
                }
            }
        }

        void SetEditorName(String MotionTrackName)
        {
            if (MotionTrackName == String.Empty)
                Name = "MotionTrackEditor: New MotionTrack";
            else
                Name = "MotionTrackEditor: " + MotionTrackName;
        }

        public void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "AddMaskObjectEffectIdentify":
                    {
                        AddMotionTrackEffectIdentify(MaskObjectEffectEditor.TheEffect.ObjectId, MaskObjectIdentifyID, true);
                    }
                    break;
                case "AddMaskObjectClipSnap":
                    {
                        AddMaskObjectClipSnap();
                    }
                    break;
                case "AddFollowObjectSlaveEffectIdentify":
                    {
                        AddMotionTrackEffectIdentify(FollowObjectEffectEditor.TheEffect.ObjectId, FollowObjectSlaveIdentifyID, false);
                    }
                    break;
            }
        }

        private void OnEffectEditorPropertySetted(object sender, PropertySettedArgs e)
        {
            if (e.OperatorProperties.ID == new OperatorId(MaskObjectEngineId, MaskObjectEffectId, MaskObjectUIId))
            {
                if (e.ValueProperty.ID == "ctrl_1" && e.ValueProperty is ValuePropertyBool)
                {
                    String identify = (e.OperatorProperties.GetParameter(MaskObjectIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value;
                    int mosaicState = (e.OperatorProperties.GetParameter(MaskObjectMosaicStateID).InterpolatedValue(0) as TypedParameterValue<int>).Value;
                    if (mosaicState == MaskObjectMosaicDisable)
                    {
                        ModifyDependObjectEffect(identify, MosaicEffectId, new int[] { MosaicAmountID }, new double[] { 0 });
                    }
                    else
                    {
                        ModifyDependObjectEffect(identify, MosaicEffectId, new int[] { MosaicAmountID }, new double[] { (e.OperatorProperties.GetParameter(MaskObjectMosaicAmountID).InterpolatedValue(0) as TypedParameterValue<Double>).Value });
                    }
                }
                else if (e.ValueProperty.ID == "ctrl_3" && e.ValueProperty is ValuePropertyBool)
                {
                    String identify = (e.OperatorProperties.GetParameter(MaskObjectIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value;
                    int blurState = (e.OperatorProperties.GetParameter(MaskObjectBlurStateID).InterpolatedValue(0) as TypedParameterValue<int>).Value;
                    if (blurState == MaskObjectBlurDisable)
                    {
                        ModifyDependObjectEffect(identify, BlurEffectId, new int[] { BlurHorizontalID, BlurVerticalID }, 
                            new double[] { 0, 0});
                    }
                    else
                    {
                        ModifyDependObjectEffect(identify, BlurEffectId, new int[] { BlurHorizontalID, BlurVerticalID },
                            new double[] { (e.OperatorProperties.GetParameter(MaskObjectBlurHorizontalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value,
                            (e.OperatorProperties.GetParameter(MaskObjectBlurVerticalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value});
                    }
                }
            }
            else if (e.OperatorProperties.ID == new OperatorId(FollowObjectSlaveEngineId, FollowObjectSlaveEffectId, FollowObjectSlaveUIId))
            {
                String identify = (e.OperatorProperties.GetParameter(FollowObjectSlaveIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value;
                if (e.ValueProperty.ID == "ctrl_1" && e.ValueProperty is ValuePropertyDouble)
                {
                    double horizontal = (e.OperatorProperties.GetParameter(FollowObjectHorizontalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                    double vertical = (e.OperatorProperties.GetParameter(FollowObjectVerticalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                    double width = (e.OperatorProperties.GetParameter(FollowObjectWidthID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                    double height = (e.OperatorProperties.GetParameter(FollowObjectHeightID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                    ModifyDependObjectEffect(identify, FollowObjectEffectId, new int[] { FollowObjectHorizontalID, FollowObjectVerticalID, FollowObjectWidthID, FollowObjectHeightID },
                            new double[] { horizontal, vertical, width, height});
                    uint trackIndex = 0; uint clipIndex = 0;
                    if (MotionPath != null && MotionPath.Count == 3 && FindDependTrackClip(identify, ref trackIndex, ref clipIndex))
                    {
                        List<Point> pointList = new List<Point>();
                        pointList.Add(MotionPath[0]);
                        pointList.Add(new Point(horizontal, vertical));
                        ITrack iTrack = _parentSequence.GetTrack(trackIndex);
                        PipRectParam rectParam = CalcFrameParam(iTrack.GetClip(clipIndex), horizontal, vertical, width, height);
                        pointList.Add(new Point(rectParam.Width / motionCanvasSize.Width, rectParam.Height / motionCanvasSize.Height));
                        MotionPath = pointList;
                    }
                }
            }
        }

        void ModifyDependObjectEffect(String identify, Guid effectId, int[] parameterId, double[] value)
        {
            if (parameterId.Length != value.Length)
                return;

            ModifyObjectEffect(FindDependClipEffect(identify, effectId), parameterId, value);
        }

        void ModifyObjectEffect(IEffect iEffect, int[] parameterId, double[] value)
        {
            if (iEffect == null)
                return;

            List<OperatorInterpolationAction> actions = new List<OperatorInterpolationAction>();
            IOperatorProperty operatorProperty = iEffect.GetProperty(SourceCategories.Video);
            for (int index = 0; index < parameterId.Length; ++index)
            {
                double currentValue = (operatorProperty.GetParameter(parameterId[index]).InterpolatedValue(PreviewModel.Position) as TypedParameterValue<Double>).Value;
                if (currentValue != value[index])
                {
                    OperatorInterpolationAction action = new OperatorInterpolationAction();
                    action.type = OperatorInterpolationActionType.SetValue;
                    action.ParameterID = parameterId[index];
                    action.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, value[index]), PreviewModel.Position),
                                                    null, InterpolationTypes.none, TimeInterpolation.linear);
                    actions.Add(action);
                }
            }

            if (actions.Count <= 0)
                return;

            ICommandRegistry cmdRegistry = PublicInterface.Get<ICommandRegistry>();
            IEffectInterpolationCommand command = cmdRegistry.CreateCommand<IEffectInterpolationCommand>();
            command.SetActions(actions.ToArray());
            command.EffectId = iEffect.ObjectId;
            command.EffectDuration = TlRecOut - TlRecIn;
            command.KeySnapTolerance = VideoFormat.FrameLength;
            command.Simulate = false;
            CommandSequence.Execute(command);
        }

        ITrack    _parentTrack = null;
        ISequence _parentSequence = null;
        ISequence _rootSequence = null;
        UInt32 _trackIndex = 0;
        UInt32 _clipIndex = 0;

        public MTime TlMarkIn { get; set; } //from timeline, start of visible range in the time of timeline
        public MTime TlMarkOut { get; set; } //from timeline, end of visible range in the time of timeline
        public MTime TlRecIn { get; set; } //from timeline, start of visible range in the time of clip(file)
        public MTime TlRecOut { get; set; } //from timeline, end of visible range in the time of clip(file)

        MTime GetPosInClip(MTime posInTimeline)
        {
            return (posInTimeline - TlRecIn + TlMarkIn);
        }

        MTime GetPosInClipRel(MTime posInTimeline)
        {
            return (posInTimeline - TlRecIn);
        }

        #endregion

        #region Document

        IDocument _document;
        uint _initialDocumentVersion = uint.MaxValue;
        public IDocument Document
        {
            get { return _document; }
        }

        void CloseDocument()
        {
            if (_document != null)
            {
                _document.Changed -= OnDocumentChanged;
            }
            _document = null;
            _initialDocumentVersion = uint.MaxValue;
        }

        public void SetDocument(IDocument document, ID sourceID)
        {
            CloseDocument();
            _initialDocumentVersion = uint.MaxValue;
            if (document == null)
                return;

            _document = document;
            _document.Refresh();
            _document.Changed += OnDocumentChanged;

            SourceID = sourceID;
            if (_previewModel != null)
            {
                if (_parentSequence != null)
                    _previewModel.SetClip (_parentSequence,InitialViewTime);
                else
                    _previewModel.SetDocument(_document);
            }

            _initialDocumentVersion = _document.DocumentVersion;

            UpdatePreviewRange();
            UpdateSections();
        }

        protected delegate void OnDocumentChangedDelegate(IDocument Document, DocumentChangedEventArgs changedArgs);
        void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            IDocument document = sender as IDocument;
            if ((null != _document) && (_document == document)) 
            {
                if (null != _uiDispatcher)
                {
                    _uiDispatcher.BeginInvoke(DispatcherPriority.Send, new OnDocumentChangedDelegate(OnDocumentChangedUIThread), document, e);
                };
            };
        }

        private void OnDocumentChangedUIThread(IDocument Document, DocumentChangedEventArgs changedArgs)
        {
            if (changedArgs.ChangeType == DocumentChangeType.UndoRedo)
            {
                OnUndoRedo(Document, changedArgs);
                return;
            }
            if ((null == _document) && (_document != Document))
                return;
            IList<ObjectChangedInfo> changes = Document.Refresh();
            if (changes == null)
                return;

            foreach (ObjectChangedInfo change in changes)
            {
                IObjectSnapshot iOS = Document.GetObject(change.ObjectId);
                IEffect iE = null;
                IEffectCollection iEC = null;
                ITransition iT = null;
                ITrack iTrack = null;

                if (iOS != null)
                {
                    iE = iOS.As<IEffect>();
                    iEC = iOS.As<IEffectCollection>();
                    iT = iOS.As<ITransition>();
                    iTrack = iOS.As<ITrack>();
                }

                switch (change.ChangeType)
                {
                    case ObjectChangeType.Added:
                        {
                            if (iE != null)
                            {
                                if (IsCurrentClipEffect(iE))
                                {
                                    if (iE.GetProperty(SourceCategories.Video).ID.Operator == MaskObjectEffectId && (iE.GetProperty(SourceCategories.Video).GetParameter(MaskObjectIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value.Length == 0)
                                    {
                                        OnAddObject(iE, FxMaskObjects, MaskObjectEffectEditor);
                                        SelectMaskIndex = FxMaskObjects.Count - 1;
                                        OnPropertyChanged("AddMaskObjectEffectIdentify");
                                    }
                                    else if (iE.GetProperty(SourceCategories.Video).ID.Operator == FollowObjectSlaveEffectId && (iE.GetProperty(SourceCategories.Video).GetParameter(FollowObjectSlaveIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value.Length == 0)
                                    {
                                        OnAddObject(iE, FxFollowObjects, FollowObjectEffectEditor);
                                        SelectedFollowIndex = FxFollowObjects.Count - 1;
                                        OnPropertyChanged("AddFollowObjectSlaveEffectIdentify");
                                    }
                                }
                            }
                            else if (iTrack != null)
                            {
                                if (iTrack.GetMotionTrack(0) != null)
                                {
                                    AddMaskObjectInput(iTrack);
                                    UpdateFollowObjectInput(iTrack);
                                }
                            }
                        }
                        break;
                    case ObjectChangeType.Modified:
                        {
                            bool isSimulate = false;
                            if (changedArgs.ChangeType == DocumentChangeType.Modifed_Simulate)
                            {
                                isSimulate = true;
                            }
                            if (iE != null)
                            {
                                if (iE.GetProperty(SourceCategories.Video).ID.Operator == MaskObjectEffectId)
                                {
                                    uint trackIndex = 0; uint clipIndex = 0;
                                    String identifyId = (iE.GetProperty(SourceCategories.Video).GetParameter(MaskObjectIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value;
                                    if (!FindDependTrackClip(identifyId, ref trackIndex, ref clipIndex))
                                    {
                                        OnPropertyChanged("AddMaskObjectClipSnap");
                                    }
                                    else
                                    {
                                        UpdateInputProp(identifyId);
                                    }
                                    if (!isSimulate)
                                    {
                                        M_MaskObjectEffectEditor.UpdateValues();
                                    }
                                    RefreshObjName(FxMaskObjects, iE.ObjectId, iE.Name);
                                }
                                else if (iE.GetProperty(SourceCategories.Video).ID.Operator == FollowObjectSlaveEffectId)
                                {
                                    uint trackIndex = 0; uint clipIndex = 0;
                                    String identifyId = (iE.GetProperty(SourceCategories.Video).GetParameter(FollowObjectSlaveIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value;
                                    if (!FindDependTrackClip(identifyId, ref trackIndex, ref clipIndex))
                                    {
                                        AddFollowObjectInput(identifyId, iE);
                                    }
                                    else
                                    {
                                        UpdateInputProp(identifyId);
                                    }
                                    if (!isSimulate)
                                    {
                                        M_FollowObjectEffectEditor.UpdateValues();
                                    }
                                    RefreshObjName(FxFollowObjects, iE.ObjectId, iE.Name);
                                }
                                RefreshInputName(iE);
                                RefreshCurEffect();
                                RefreshObjTimelinePostion();
                                OnPropertyChanged("EffectName");
                            }
                        }
                        break;
                    case ObjectChangeType.Deleted:
                        {
                            _bIndexSyn = false;
                            int pre_inspectorIndex = _inspector_SelectedIndex;
                            bool isMotionTrackEffectDelete = false;
                            int nIndex = FxMaskObjects.GetEffectIndex(change.ObjectId);
                            if (nIndex >= 0 && nIndex < FxMaskObjects.Count)
                            {
                                FxMaskObjects[nIndex].DeleteEffectPreviewEvent -= OnDeleteEffectPreview;
                                FxMaskObjects.RemoveAt(nIndex);
                                _selectMaskIndex = -1;
                                SelectMaskIndex = Math.Min(_pre_selectedMaskIndex, FxMaskObjects.Count - 1);
                                isMotionTrackEffectDelete = true;
                            }
                            nIndex = FxFollowObjects.GetEffectIndex(change.ObjectId);
                            if (nIndex >= 0 && nIndex < FxFollowObjects.Count)
                            {
                                FxFollowObjects[nIndex].DeleteEffectPreviewEvent -= OnDeleteEffectPreview;
                                FxFollowObjects.RemoveAt(nIndex);
                                _selectedFollowIndex = -1;
                                SelectedFollowIndex = Math.Min(_pre_selectedFollowIndex, FxFollowObjects.Count - 1);
                                isMotionTrackEffectDelete = true;
                            }
                            _bIndexSyn = true;
                            if (isMotionTrackEffectDelete)
                            {
                                Inspector_SelectedIndex = Math.Min(pre_inspectorIndex, _inputPropList.Count - 1);
                                OnPropertyChanged("InputList");
                                OnPropertyChanged("InputPropList");
                            }
                        }
                        break;
                }
            }
            if (changedArgs.ChangedObjectIds.Count > 0)
            {
                OnPropertyChanged("Dirty");
            }
        }

        private void OnUndoRedo(IDocument Document, DocumentChangedEventArgs changedArgs)
        {
            if ((null == _document) && (_document != Document))
            {
                return;
            }
            IList<ObjectChangedInfo> changes = Document.Refresh();
            if (changes == null)
            {
                return;
            }

            foreach (ObjectChangedInfo change in changes)
            {
                IObjectSnapshot iOS = Document.GetObject(change.ObjectId);
                IEffect iE = null;
                IEffectCollection iEC = null;
                ITransition iT = null;
                ITrack iTrack = null;
                IClip iClip = null;

                if (iOS != null)
                {
                    iE = iOS.As<IEffect>();
                    iEC = iOS.As<IEffectCollection>();
                    iT = iOS.As<ITransition>();
                    iTrack = iOS.As<ITrack>();
                    iClip = iOS.As<IClip>();
                }

                switch (change.ChangeType)
                {
                    case ObjectChangeType.Added:
                        {
                            if (iE != null)
                            {
                                if (IsCurrentClipEffect(iE))
                                {
                                    if (iE.GetProperty(SourceCategories.Video).ID.Operator == MaskObjectEffectId)
                                    {
                                        OnAddObject(iE, FxMaskObjects, MaskObjectEffectEditor);
                                        SelectMaskIndex = FxMaskObjects.Count - 1;
                                    }
                                    else if (iE.GetProperty(SourceCategories.Video).ID.Operator == FollowObjectSlaveEffectId)
                                    {
                                        OnAddObject(iE, FxFollowObjects, FollowObjectEffectEditor);
                                        SelectedFollowIndex = FxFollowObjects.Count - 1;
                                        String identifyId = (iE.GetProperty(SourceCategories.Video).GetParameter(FollowObjectSlaveIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value;
                                        AddFollowObjectInput(identifyId, iE);
                                    }
                                }
                            }
                            if (iTrack != null)
                            {
                                IEffect effect = FindDependMotionTrackEffect(iTrack.GetMotionTrack(0).GUID.ToString(), MaskObjectEffectId);
                                if (effect != null)
                                {
                                    AddMaskObjectInput(iTrack);
                                }
                                effect = FindDependMotionTrackEffect(iTrack.GetMotionTrack(0).GUID.ToString(), FollowObjectSlaveEffectId);
                                if (effect != null)
                                {
                                    UpdateFollowObjectInput(iTrack);
                                }
                            }
                        }
                        break;
                    case ObjectChangeType.Modified:
                        {
                            if (iE != null)
                            {
                                RefreshInputName(iE);
                                RefreshCurEffect();
                                RefreshSelector();
                                RefreshDrawingItem();
                                RefreshObjTimelinePostion();
                                RefreshEffectName(iE, FxMaskObjects);
                                RefreshEffectName(iE, FxFollowObjects);
                                MaskObjectEffectEditor.UpdateValues();
                                FollowObjectEffectEditor.UpdateValues();
                                OnPropertyChanged("EffectName");
                            }
                        }
                        break;
                    case ObjectChangeType.Deleted:
                        {
                            for (int i = 0; i < _inputPropList.Count; i++)
                            {
                                var obj = _inputPropList[i];
                                if (obj.TrackObjectId == change.ObjectId)
                                {
                                    _inputPropList[i].AllowedMedia = DropZoneControlMedia.NoEffect | DropZoneControlMedia.SupportVisiableMedia;
                                    _inputPropList[i].FollowObjectHintVisibility = Visibility.Visible;
                                    _inputPropList[i].Value = new InputDesc(0, null, 0, false, null);
                                    _inputList[i] = _inputPropList[i].Value;

                                    OnPropertyChanged("InputList");
                                    OnPropertyChanged("InputPropList");
                                }
                            }
                            _bIndexSyn = false;
                            int pre_inspectorIndex = _inspector_SelectedIndex;
                            bool isMotionTrackEffectDelete = false;
                            int nIndex = FxMaskObjects.GetEffectIndex(change.ObjectId);
                            if (nIndex >= 0 && nIndex < FxMaskObjects.Count)
                            {
                                OnDeleteEffect(FxMaskObjects[nIndex].ObjectID);
                                FxMaskObjects[nIndex].DeleteEffectPreviewEvent -= OnDeleteEffectPreview;
                                FxMaskObjects.RemoveAt(nIndex);
                                _selectMaskIndex = -1;
                                SelectMaskIndex = Math.Min(_pre_selectedMaskIndex, FxMaskObjects.Count - 1);
                                isMotionTrackEffectDelete = true;
                            }
                            nIndex = FxFollowObjects.GetEffectIndex(change.ObjectId);
                            if (nIndex >= 0 && nIndex < FxFollowObjects.Count)
                            {
                                OnDeleteEffect(FxFollowObjects[nIndex].ObjectID);
                                FxFollowObjects[nIndex].DeleteEffectPreviewEvent -= OnDeleteEffectPreview;
                                FxFollowObjects.RemoveAt(nIndex);
                                _selectedFollowIndex = -1;
                                SelectedFollowIndex = Math.Min(_pre_selectedFollowIndex, FxFollowObjects.Count - 1);
                                isMotionTrackEffectDelete = true;
                            }
                            _bIndexSyn = true;
                            if (isMotionTrackEffectDelete)
                            {
                                Inspector_SelectedIndex = Math.Min(pre_inspectorIndex, _inputPropList.Count - 1);
                                OnPropertyChanged("InputList");
                                OnPropertyChanged("InputPropList");
                            }
                        }
                        break;
                }
            }
            if (changedArgs.ChangedObjectIds.Count > 0)
            {
                OnPropertyChanged("Dirty");
            }
        }

        private bool IsCurrentClipEffect(IEffect iEffect)
        {
            IEffectCollection iEffectCollection = _parentTrack.GetClipMotionEffects(_clipIndex);
            if (iEffectCollection == null)
            {
                return false;
            }
            foreach (IEffect effect in iEffectCollection)
            {
                if (effect.ObjectId == iEffect.ObjectId)
                {
                    return true;
                }
            }

            return false;
        }

        public override bool Dirty
        {
            get { return (_document != null && _document.IsChangedFromVersion(_initialDocumentVersion)); }
        }

        #endregion

        #region Property Bags

        private int _motionTabSelectIndex = 0;
        public int MotionTabSelectIndex
        {
            get
            {
                return _motionTabSelectIndex;
            }
            set
            {
                if (_motionTabSelectIndex != value)
                {
                    _motionTabSelectIndex = value;
                    OnPropertyChanged("MotionTabSelectIndex");
                }
            }
        }

        public string MediaFilename
        {
            get
            {
                if (Document == null)
                {
                    return string.Empty;
                }
                string strRet = string.Empty;
                ISource Source = Document.GetObject(_sourceID).As<ISource>();
                if (Source != null)
                {
                    ICategories categories = Source.As<ICategories>();
                    IMediaObject mediaObject = null;
                    if (categories != null)
                    {
                        IMediaExtern externClip = Source.As<IMediaExtern>();
                        if (externClip != null)
                        {
                            if ((categories.Categories & SourceCategories.Video) > 0)
                            {
                                mediaObject = externClip.GetMediaObject(SourceCategories.Video);
                            }
                            else if ((categories.Categories & SourceCategories.Audio) > 0)
                            {
                                mediaObject = externClip.GetMediaObject(SourceCategories.Audio);
                            }
                            if (mediaObject != null)
                            {
                                strRet = System.IO.Path.GetFileName(mediaObject.TargetPath).ToUpper();
                            }
                        }
                    }
                }
                return strRet;
            }
        }

        // MaskObject Effect Editor
        protected EffectModel M_MaskObjectEffectEditor;
        public IEffectModel MaskObjectEffectEditor
        {
            get
            {
                return M_MaskObjectEffectEditor;
            }
        }

        // FollowObject Effect Editor
        protected EffectModel M_FollowObjectEffectEditor;
        public IEffectModel FollowObjectEffectEditor
        {
            get
            {
                return M_FollowObjectEffectEditor;
            }
        }

        private FxStackEffectsCollection _fx_maskobjects = new FxStackEffectsCollection();
        public FxStackEffectsCollection FxMaskObjects
        {
            get
            {
                return _fx_maskobjects;
            }
        }

        private FxStackEffectsCollection _fx_followobjects = new FxStackEffectsCollection();
        public FxStackEffectsCollection FxFollowObjects
        {
            get
            {
                return _fx_followobjects;
            }
        }

        private bool _bIndexSyn = true;

        private int _pre_selectedMaskIndex = 0;
        private int _selectMaskIndex = -1;
        public int SelectMaskIndex
        {
            get
            {
                return _selectMaskIndex;
            }
            set
            {
                if (_selectMaskIndex != value)
                {
                    if (value == -1)
                    {
                        _pre_selectedMaskIndex = _selectMaskIndex;
                    }
                    _selectMaskIndex = value;
                    MotionTabSelectIndex = 0;
                    if (_bIndexSyn)
                    {
                        Inspector_SelectedIndex = SubIndexToInspectorIndex(value, MaskObjectEffectId);
                    }
                    ResetCollectionModel(FxMaskObjects, _selectMaskIndex, MaskObjectEffectEditor);
                    OnPropertyChanged("SelectMaskIndex");
                }
            }
        }

        private int _pre_selectedFollowIndex = 0;
        private int _selectedFollowIndex = -1;
        public int SelectedFollowIndex
        {
            get
            {
                return _selectedFollowIndex;
            }
            set
            {
                if (_selectedFollowIndex != value)
                {
                    if (value == -1)
                    {
                        _pre_selectedFollowIndex = _selectedFollowIndex;
                    }
                    _selectedFollowIndex = value;
                    MotionTabSelectIndex = 1;
                    if (_bIndexSyn)
                    {
                        Inspector_SelectedIndex = SubIndexToInspectorIndex(value, FollowObjectSlaveEffectId);
                    }
                    ResetCollectionModel(FxFollowObjects, _selectedFollowIndex, FollowObjectEffectEditor);
                    OnPropertyChanged("SelectedFollowIndex");
                }
            }
        }

        private int _inspector_SelectedIndex = -1;
        public int Inspector_SelectedIndex
        {
            get
            {
                return _inspector_SelectedIndex;
            }
            set
            {
                if (_inspector_SelectedIndex != value)
                {
                    _inspector_SelectedIndex = value;
                    RefreshCurEffect();
                    RefreshSelector();
                    RefreshEffectEditor();
                    RefreshDrawingItem();
                    RefreshObjTimelinePostion();
                    RefreshSubIndex();
                    OnPropertyChanged("Inspector_SelectedIndex");
                }
            }
        }

        public IEffect CurEffect
        {
            get;
            set;
        }

        public IOperatorProperty CurProperty
        {
            get;
            set;
        }

        private void RefreshObjName(FxStackEffectsCollection collection, ID objID, string name)
        {
            foreach (var obj in collection)
            {
                if (obj.ObjectID == objID)
                {
                    obj.EffectName = name;
                }
            }
        }

        private void RefreshCurEffect()
        {
            CurEffect = GetMotionEffectByIndex(Inspector_SelectedIndex);
            if (CurEffect == null)
            {
                CurProperty = null;
                return;
            }
            CurProperty = CurEffect.GetProperty(CurEffect.Categories);
        }

        private void RefreshSubIndex()
        {
            if (!_bIndexSyn)
            {
                return;
            }
            if (CurProperty == null)
            {
                SelectMaskIndex = -1;
                SelectedFollowIndex = -1;
                return;
            }
            if (CurProperty.ID.Operator == MaskObjectEffectId)
            {
                SelectMaskIndex = InspectorIndexToSubIndex(_inspector_SelectedIndex, MaskObjectEffectId);
                MotionTabSelectIndex = 0;
            }
            else if (CurProperty.ID.Operator == FollowObjectSlaveEffectId)
            {
                SelectedFollowIndex = InspectorIndexToSubIndex(_inspector_SelectedIndex, FollowObjectSlaveEffectId);
                MotionTabSelectIndex = 1;
            }
        }

        public DrawTools.ToolType MaskLastClickTool = DrawTools.ToolType.Ellipse;
        private void RefreshSelector()
        {
            if (CurProperty == null)
            {
                MotionSelector = DrawTools.ToolType.None;
                return;
            }
            if (CurProperty.ID.Operator == MaskObjectEffectId)
            {
                CurProperty.Duration = PreviewModel.Duration;
                TypedParameterValue<byte[]> value = CurProperty.GetParameter(MaskObjectPathID).InterpolatedValue(PreviewModel.Position) as TypedParameterValue<byte[]>;
                if (value == null)
                {
                    MotionSelector = MaskLastClickTool;
                    return;
                }
                if (value.Value == null || value.Value.Length == 0)
                {
                    MotionSelector = MaskLastClickTool;
                    return;
                }
            }
            else if (CurProperty.ID.Operator == FollowObjectSlaveEffectId)
            {
                MotionSelector = DrawTools.ToolType.Target;
                return;
            }
            MotionSelector = DrawTools.ToolType.Polygon;
        }

        private void RefreshEffectEditor()
        {
            if (CurProperty == null)
            {
                return;
            }
            if (CurProperty.ID.Operator == MaskObjectEffectId)
            {
                MaskObjectEffectEditor.TheEffect = CurEffect;
            }
            else if (CurProperty.ID.Operator == FollowObjectSlaveEffectId)
            {
                FollowObjectEffectEditor.TheEffect = CurEffect;
            }
        }

        private void ResetCollectionModel(FxStackEffectsCollection collection, int nSelectedIndex, IEffectModel model)
        {
            if (collection == null)
            {
                return;
            }
            foreach (var obj in collection)
            {
                obj.EffectEditor = collection.IndexOf(obj) == nSelectedIndex ? model : null;
            }
        }

        private void RefreshObjTimelinePostion(List<Tuple<List<Point>, MTime>> list = null)
        {
            _pathItems.Clear();
            if (list == null)
            {
                if (CurProperty == null)
                {
                    goto final;
                }
                CurProperty.Duration = PreviewModel.Duration;
                TypedParameterValue<byte[]> value = CurProperty.GetParameter(CurProperty.ID.Operator == MaskObjectEffectId ? MaskObjectPathID : FollowObjectSlavePathID).InterpolatedValue(PreviewModel.Position) as TypedParameterValue<byte[]>;
                if (value == null)
                {
                    goto final;
                }
                list = ParseMotionTrackInfo(value.Value);
            }
            if (list == null || list.Count == 0)
            {
                goto final;
            }
            int nCount = list.Count;
            double begTime = -1;
            double preTime = 0;
            for (int i = 0; i < nCount; i++)
            {
                var obj = list[i];
                if (i == nCount - 1)
                {
                    if (begTime >= 0)
                    {
                        if (Math.Abs(obj.Item2 * PreviewModel.Duration - preTime - PreviewModel.M_Player.FrameLength) < PreviewModel.M_Player.FrameLength / 10)
                        {
                            _pathItems.Add(new TimelinePanelItem(begTime + TlRecIn, obj.Item2 * PreviewModel.Duration - begTime + PreviewModel.M_Player.FrameLength));
                        }
                        else
                        {
                            _pathItems.Add(new TimelinePanelItem(begTime + TlRecIn, preTime - begTime + PreviewModel.M_Player.FrameLength));
                            _pathItems.Add(new TimelinePanelItem(obj.Item2 * PreviewModel.Duration + TlRecIn, PreviewModel.M_Player.FrameLength));
                        }
                    }
                    else
                    {
                        _pathItems.Add(new TimelinePanelItem(obj.Item2 * PreviewModel.Duration + TlRecIn, PreviewModel.M_Player.FrameLength));
                    }
                    break;
                }
                if (begTime < 0)
                {
                    begTime = obj.Item2 * PreviewModel.Duration;
                    preTime = obj.Item2 * PreviewModel.Duration;
                    continue;
                }
                if (Math.Abs(obj.Item2 * PreviewModel.Duration - preTime - PreviewModel.M_Player.FrameLength) < PreviewModel.M_Player.FrameLength / 10)
                {
                    preTime = obj.Item2 * PreviewModel.Duration;
                    continue;
                }
                else
                {
                    _pathItems.Add(new TimelinePanelItem(begTime + TlRecIn, preTime - begTime + PreviewModel.M_Player.FrameLength));
                    begTime = obj.Item2 * PreviewModel.Duration;
                    preTime = begTime;
                    continue;
                }
            }
        final:
            OnPropertyChanged("PathItems");
        }

        public void DeleteMotionPath(double start, double duration)
        {
            if (CurProperty == null)
            {
                return;
            }
            CurProperty.Duration = PreviewModel.Duration;
            TypedParameterValue<byte[]> value = CurProperty.GetParameter(CurProperty.ID.Operator == MaskObjectEffectId ? MaskObjectPathID : FollowObjectSlavePathID).InterpolatedValue(PreviewModel.Position) as TypedParameterValue<byte[]>;
            if (value == null)
            {
                return;
            }
            List<Tuple<List<Point>, MTime>> list = ParseMotionTrackInfo(value.Value);
            if (list == null || list.Count == 0)
            {
                return;
            }
            start -= TlRecIn;
            double dLen = PreviewModel.M_Player.FrameLength / PreviewModel.Duration;
            double end = start + duration;
            start /= PreviewModel.Duration;
            end /= PreviewModel.Duration;
            int nIndex = 0;
            while (nIndex < list.Count)
            {
                if (start - list[nIndex].Item2 < dLen
                    && end - list[nIndex].Item2 > 0)
                {
                    list.RemoveAt(nIndex);
                }
                else
                {
                    nIndex++;
                }
            }
            OperatorInterpolationAction action = new OperatorInterpolationAction();
            action.type = OperatorInterpolationActionType.SetValue;
            action.key = new Keyframe(new ParameterPoint(new TypedParameterValue<byte[]>(ParameterType.Binary, BuildMotionTrackInfo(list)), GetPosInClipRel(PreviewModel.Position)), null, InterpolationTypes.none, TimeInterpolation.linear);
            if (CurProperty.ID.Operator == MaskObjectEffectId)
            {
                UpdateMaskObjectPath(list, action);
            }
            else if (CurProperty.ID.Operator == FollowObjectSlaveEffectId)
            {
                UpdateFollowObjectPath(list, action);
            }
        }

        private void OnAddObject(IEffect effect, FxStackEffectsCollection collection, IEffectModel model)
        {
            if (effect == null)
            {
                return;
            }
            model.AllowKeyframes = EffectModelKeyframeMode.None;
            model.ExtendedParameters = true;
            model.Duration = _previewModel.Duration;
            model.FrameLength = VideoFormat.FrameLength;
            model.Environment = new ConditionEnvironment();
            model.TheEffect = effect;
            model.ParentSequence = _parentSequence;
            model.ParentTrack = _parentTrack;
            model.ParentClip = _document.GetObject(_sourceID).As<IBaseClip>();
            IOperatorProperty property = effect.GetProperty(SourceCategories.Video);
            FxStackEffectItem item = new FxStackEffectItem(collection, _document.GetObject(SourceID).As<ISource>(), _parentSequence, _parentTrack, _document.GetObject(SourceID).As<IBaseClip>(), effect.ObjectId, CommandSequence, property.ID, EffectUsage.Effects, SourceCategories.Video, effect.Name, false, true, true, false, null);
            item.DeleteEffectPreviewEvent += OnDeleteEffectPreview;
            item.IsMotionTrackEffect = true;
            collection.Add(item);
            ResetCollectionModel(collection, collection.Count - 1, model);

            OnPropertyChanged("FxMaskObjects");
            OnPropertyChanged("FxFollowObjects");
        }

        private void RefreshEffectName(IEffect effect, FxStackEffectsCollection collection)
        {
            foreach (var obj in collection)
            {
                if (obj.ObjectID == effect.ObjectId)
                {
                    obj.EffectName = effect.Name;
                }
            }
        }

        #endregion
    
        #region inputs

        private void RefreshInputName(IEffect iE)
        {
            string identify = string.Empty;
            if (!GetEffectIdentify(iE, ref identify))
            {
                return;
            }
            foreach (var obj in _inputPropList)
            {
                if (obj.ID == identify)
                {
                    obj.DisplayName = iE.Name;
                }
            }
        }

        List<InputDesc> _inputList;
        public List<InputDesc> InputList
        {
            get { return _inputList ; }
        }

        void InitInputList() 
        {
            _inputList = new List<InputDesc>();
            _inputPropList = new List<InputValueproperty>();
            IEffectCollection iEffectCollection = _parentTrack.GetClipMotionEffects(_clipIndex);
            if (iEffectCollection == null)
            {
                return;
            }
            foreach (IEffect effect in iEffectCollection)
            {
                String identify = "";
                DropZoneControlMedia allowedMedia = DropZoneControlMedia.Video;
                if (effect.GetProperty(SourceCategories.Video).ID.Operator == MaskObjectEffectId)
                {
                    OnAddObject(effect, FxMaskObjects, MaskObjectEffectEditor);
                    allowedMedia = DropZoneControlMedia.NoEffect;
                    identify = (effect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value;
                }
                else if (effect.GetProperty(SourceCategories.Video).ID.Operator == FollowObjectSlaveEffectId)
                {
                    OnAddObject(effect, FxFollowObjects, FollowObjectEffectEditor);
                    allowedMedia = DropZoneControlMedia.NoEffect | DropZoneControlMedia.SupportVisiableMedia;
                    identify = (effect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectSlaveIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value;
                }
                else
                    continue;
                InputValueproperty prop = new InputValueproperty(identify, string.Empty, string.Empty, null, true, true, false, "DropZone", InputsChanged, effect.Name);
                prop.AllowedMedia = allowedMedia;
                prop.ObjectId = effect.ObjectId;

                uint trackIndex = 0; uint clipIndex = 0;
                _inputPropList.Add(prop);
                if (FindDependTrackClip(identify, ref trackIndex, ref clipIndex))
                {
                    ITrack iTrack = _parentSequence.GetTrack(trackIndex);
                    ID[] ids = { iTrack.GetClip(clipIndex).ObjectId, iTrack.ObjectId };
                    prop.Value = new InputDesc(0, new ObjectsInDocument(Document, ids), 0, false, GetInputFilterList(trackIndex, clipIndex));
                }
                else
                {
                    prop.Value = new InputDesc(0, null, 0, false, null);
                }

                _inputList.Add(prop.Value);
            }

            OnPropertyChanged("InputList");
            OnPropertyChanged("InputPropList");
        }

        private List<InputValueproperty> _inputPropList;
        public List<InputValueproperty> InputPropList { get { return _inputPropList; } }

        private void AddMaskObjectInput(ITrack iTrack)
        {
            String guid = iTrack.GetMotionTrack(0).GUID.ToString();
            IEffectCollection iEffectCollection = _parentTrack.GetClipMotionEffects(_clipIndex);
            if (iEffectCollection == null)
            {
                return;
            }
            foreach (IEffect effect in iEffectCollection)
            {
                if (effect.GetProperty(SourceCategories.Video).ID.Operator == MaskObjectEffectId && 
                    (effect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value == guid)
                {
                    InputValueproperty prop = new InputValueproperty(iTrack.GetMotionTrack(0).GUID.ToString(), string.Empty, string.Empty, null, true, true, false, "DropZone", InputsChanged, effect.Name);
                    prop.AllowedMedia = DropZoneControlMedia.NoEffect;
                    prop.ObjectId = effect.ObjectId;
                    _inputPropList.Add(prop);
                    ID[] ids = { iTrack.GetClip(0).ObjectId, iTrack.ObjectId };
                    prop.Value = new InputDesc(0, new ObjectsInDocument(Document, ids), 0, false, GetInputFilterList(_parentSequence.GetTrackIndex(iTrack), 0));
                    _inputList.Add(prop.Value);
                    OnPropertyChanged("InputPropList");
                    return;
                }
            }
        }

        private void AddFollowObjectInput(String identifyId, IEffect iE)
        {
            foreach (InputValueproperty property in _inputPropList)
            {
                if (property.ID == identifyId)
                {
                    return;
                }
            }

            InputValueproperty prop = new InputValueproperty(identifyId, string.Empty, string.Empty, null, true, true, false, "DropZone", InputsChanged, iE.Name);
            prop.ObjectId = iE.ObjectId;
            prop.AllowedMedia = DropZoneControlMedia.NoEffect | DropZoneControlMedia.SupportVisiableMedia;
            prop.FollowObjectHintVisibility = Visibility.Visible;
            _inputPropList.Add(prop);
            prop.Value = new InputDesc(0, null, 0, false, null);
            _inputList.Add(prop.Value);
            OnPropertyChanged("InputPropList");
        }

        private void UpdateFollowObjectInput(ITrack iTrack)
        {
            String guid = iTrack.GetMotionTrack(0).GUID.ToString();
            for (int i = 0; i < _inputList.Count; i++)
            {
                if (_inputPropList[i] != null && _inputPropList[i].ID == guid)
                {
                    IBaseClip baseClip = iTrack.GetClip(0);
                    ID[] ids = { baseClip.ObjectId, iTrack.ObjectId };
                    if (MotionPath != null)
                    {
                        IEffect iEffect = FindDependClipEffect(guid, FollowObjectEffectId);
                        if (iEffect != null)
                        {
                            double horizontal = (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectHorizontalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                            double vertical = (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectVerticalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                            double width = (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectWidthID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                            double height = (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectHeightID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                            List<Point> pointList = new List<Point>();
                            pointList.Add(MotionPath[0]);
                            pointList.Add(new Point(horizontal, vertical));
                            PipRectParam rectParam = CalcFrameParam(baseClip, horizontal, vertical, width, height);
                            pointList.Add(new Point(rectParam.Width / motionCanvasSize.Width, rectParam.Height / motionCanvasSize.Height));
                            MotionPath = pointList;
                        }
                    }
                    _inputPropList[i].Value = new InputDesc(0, new ObjectsInDocument(Document, ids), 0, false, GetInputFilterList(_parentSequence.GetTrackIndex(iTrack), 0));
                    _inputPropList[i].FollowObjectHintVisibility = Visibility.Collapsed;
                    _inputPropList[i].TrackObjectId = iTrack.ObjectId;
                    OnPropertyChanged("InputPropList");
                    return;
                }
            }
        }

        private void UpdateInputProp(String identify)
        {
            for (int i = 0; i < _inputList.Count; i++)
            {
                if (_inputPropList[i] != null && _inputPropList[i].ID == identify)
                {
                    uint trackIndex = 0; uint clipIndex = 0;
                    if (FindDependTrackClip(identify, ref trackIndex, ref clipIndex))
                    {
                        ITrack iTrack = _parentSequence.GetTrack(trackIndex);
                        ID[] ids = { iTrack.GetClip(0).ObjectId, iTrack.ObjectId };
                        _inputPropList[i].Value = new InputDesc(0, new ObjectsInDocument(Document, ids), 0, false, GetInputFilterList(trackIndex, clipIndex));
                        OnPropertyChanged("InputPropList");
                        return;
                    }
                }
            }
        }

        private ObservableCollection<IOperatorProperty> GetInputFilterList(uint trackIndex, uint clipIndex)
        {
            ObservableCollection<IOperatorProperty> result = new ObservableCollection<IOperatorProperty>();
            result.CollectionChanged += InputFilterListChanged;
            ITrack iTrack = _parentSequence.GetTrack(trackIndex);
            IEffectCollection iEC = iTrack.GetClipEffects(clipIndex);
            if (iEC == null)
                return result;
            result.CollectionChanged -= InputFilterListChanged;
            foreach (IEffect iE in iEC)
            {
                if (iE == null)
                    continue;
                IOperatorProperty iEP = iE.GetProperty(SourceCategories.Video);
                if (iEP == null)
                    iEP = iE.GetProperty(SourceCategories.Audio);
                if (iEP == null)
                    continue;
                result.Add(iEP);
            }
            result.CollectionChanged += InputFilterListChanged;
            return result;
        }

        private void InputFilterListChanged(Object sender, NotifyCollectionChangedEventArgs e)
        {
            InputValueproperty changed = null;
            for (int i = 0; i < _inputList.Count && changed == null; i++)
                if (_inputPropList[i] != null && _inputPropList[i].Operators != null && sender == _inputPropList[i].Operators)
                    changed = _inputPropList[i];
            if (changed == null  || changed.Changing)
                return;

            SetInputFilters(changed.ID, changed.Value.Operators);
        }

        private void InputsChanged(ValuePropertyBase sender, string propertyName)
        {
            if (_inputList == null)
                return;
            if (sender is InputValueproperty)
            {
                InputValueproperty ivp = sender as InputValueproperty;
                if (ivp.Value == null)
                    return;
                if (ivp.Value.InputIndex > _inputList.Count)
                    return;
                
                if (propertyName == "ObjectInDocument")
                {
                    if (ivp.Value.ObjectInDocument != null && ivp.Value.ObjectInDocument.Document != _document)
                        SetInput(ivp.ObjectId, ivp.ID, ivp.DBRecord);
                }
                else if (propertyName == "DisplayName")
                {
                    IEffectCollection collection = _parentTrack.GetClipMotionEffects(_clipIndex);
                    if (collection == null)
                    {
                        return;
                    }
                    string identify = string.Empty;
                    foreach (IEffect iE in collection)
                    {
                        if (!GetEffectIdentify(iE, ref identify))
                        {
                            continue;
                        }
                        if (identify == ivp.ID && iE.Name != ivp.DisplayName)
                        {
                            OnEffectNameChanged(iE.ObjectId, ivp.DisplayName);
                        }
                    }
                }
            }
        }
        
        private void SetInput(ID effectId, String identify, DbRecord dbRecord)
        {
            if (dbRecord == null)
                return;

            uint trackIndex = 0; uint clipIndex = 0;
            if (!FindDependTrackClip(identify, ref trackIndex, ref clipIndex))
            {
                AddFollowObjectClip(effectId, dbRecord);
            }
            else
            {
                ReplaceFollowObjectClip(effectId, dbRecord, trackIndex, clipIndex);
            }
        }
 
        private void SetInputFilters(String identify, IList<IOperatorProperty> filterProperties)
        {
            if (identify.Length > 0)
            {
                uint trackIndex = 0; uint clipIndex = 0;
                if (FindDependTrackClip(identify, ref trackIndex, ref clipIndex))
                {
                    ICommandRegistry cmdRegistry = PublicInterface.Get<ICommandRegistry>();
                    ISetTrackEffectCommand setTrackEffectCommand = cmdRegistry.CreateCommand<ISetTrackEffectCommand>();
                    setTrackEffectCommand.SetSequence(_parentSequence.ObjectId);
                    setTrackEffectCommand.SetEditMode(SequenceEditMode.Alternate);
                    setTrackEffectCommand.AddTrackIndex(trackIndex);
                    setTrackEffectCommand.AddClipIndex(clipIndex);
                    setTrackEffectCommand.FilterProperties = filterProperties;
                    setTrackEffectCommand.Simulate = false;
                    CommandSequence.Execute(setTrackEffectCommand);
                }
            }
        }

        #endregion

        #region Intro / Emphasis / Extro

        // sectionPoints hold start/end points of the sections. All time are relative to markin.
        // sectionPoints[0] is a helper point set to 0
        // sectionPoints[1..length-2] are real section points coming from the formatter anchors 
        // sectionPoints[length-1] is a helper point set to markout-markin
        private List<MTime> sectionPoints = new List<MTime> ();

        MTime UIToFormatterTime(MTime uitime)
        {
            return uitime - _previewModel.StartPos ;
        }
        MTime FormatterToUITime (MTime formattertime)
        {
            return formattertime + _previewModel.StartPos ;
        }

        private void UpdateSections ()
        {
            // get new sections points
            IObjectSnapshot objSnap = _document.GetObject(_sourceID);
            if (objSnap.Is<IBaseClip>())
            {
                IBaseClip iBaseClip = objSnap.As<IBaseClip>();
                sectionPoints.Clear();
                sectionPoints.Add(0);
                sectionPoints.Add(iBaseClip.ClipDuration);
            }
            UpdateSectionProperties();
        }

        private void UpdateSectionProperties ()
        {
            if (_PreviewDuration <= 0)
                return ;
            OnPropertyChanged("ClipStartTime");
            OnPropertyChanged("ClipDuration");
        }

        public double ClipStartTime    { get { return FormatterToUITime(sectionPoints[0]); } }
        public double ClipDuration { get { return sectionPoints[1] - sectionPoints[0]; } }

        private List<TimelinePanelItem> _pathItems = new List<TimelinePanelItem>();
        public List<TimelinePanelItem> PathItems
        {
            get
            {
                return _pathItems;
            }
        }

        #endregion

        public void OnEffectNameChanged(ID objId, string newName)
        {
            IObjectSnapshot ios = _document.GetObject(objId);
            if (ios == null || ios.As<IEffect>() == null)
            {
                return;
            }
            IEffect effect = ios.As<IEffect>();
            uint trackIndex = 0, clipIndex = 0;
            if (FindDependTrackClip(effect, ref trackIndex, ref clipIndex))
            {
                ICommandRegistry cmdRegistry = PublicInterface.Get<ICommandRegistry>();
                Avid.PinnacleServices.Commands.Sequence.ISequenceModifyTrackCommand cmd = cmdRegistry.CreateCommand<Avid.PinnacleServices.Commands.Sequence.ISequenceModifyTrackCommand>();

                cmd.TrackIndex = trackIndex;
                cmd.TrackName = newName;
                cmd.SetSequence(_parentSequence.ObjectId);
                cmd.SetEditMode(SequenceEditMode.Smart);
                cmd.RenameObject = true;
                cmd.RenameObjectID = objId;

                if (CommandSequence.TryLockContext(cmd, 0))
                {
                    CommandSequence.Execute(cmd);
                }
            }
            else
            {
                ICommandRegistry cmdRegistry = PublicInterface.Get<ICommandRegistry>();
                Avid.PinnacleServices.Commands.IObjectSetNameCommand cmd = cmdRegistry.CreateCommand<Avid.PinnacleServices.Commands.IObjectSetNameCommand>();

                cmd.SetObject(objId);
                cmd.ObjectName = newName;

                if (CommandSequence.TryLockContext(cmd, 0))
                {
                    CommandSequence.Execute(cmd);
                }
            }
        }

        private int InspectorIndexToSubIndex(int nInspectorIndex, Guid subID)
        {
            IEffectCollection collection = _parentTrack.GetClipMotionEffects(_clipIndex);
            if (collection == null)
            {
                return -1;
            }
            int nIndex = 0;
            int nSubIndex = 0;
            foreach (IEffect effect in collection)
            {
                IOperatorProperty iop = effect.GetProperty(effect.Categories);
                if (iop.ID.Operator == MaskObjectEffectId || iop.ID.Operator == FollowObjectSlaveEffectId)
                {
                    if (iop.ID.Operator == subID)
                    {
                        if (nInspectorIndex == nIndex)
                        {
                            return nSubIndex;
                        }
                        nSubIndex++;
                    }
                    nIndex++;
                }
            }
            return -1;
        }

        private int SubIndexToInspectorIndex(int nMaskIndex, Guid subID)
        {
            IEffectCollection collection = _parentTrack.GetClipMotionEffects(_clipIndex);
            if (collection == null)
            {
                return -1;
            }
            int nIndex = 0;
            int nSub = 0;
            foreach (IEffect effect in collection)
            {
                IOperatorProperty iop = effect.GetProperty(effect.Categories);
                if (iop.ID.Operator == MaskObjectEffectId || iop.ID.Operator == FollowObjectSlaveEffectId)
                {
                    if (iop.ID.Operator == subID)
                    {
                        if (nMaskIndex == nSub)
                        {
                            return nIndex;
                        }
                        nSub++;
                    }
                    nIndex++;
                }
            }
            return -1;
        }

        private DrawTools.ToolType _motionSelector;
        public DrawTools.ToolType MotionSelector
        {
            get
            {
                return _motionSelector;
            }
            set
            {
                if (_motionSelector != value)
                {
                    _motionSelector = value;
                    OnPropertyChanged("MotionSelector");
                }
            }
        }

        private DrawTools.ToolType _motionBtnVis;
        public DrawTools.ToolType MotionBtnVis
        {
            get
            {
                return _motionBtnVis;
            }
            set
            {
                if (_motionBtnVis != value)
                {
                    _motionBtnVis = value;
                    OnPropertyChanged("MotionBtnVis");
                }
            }
        }

        private List<Point> _motionPath;
        public List<Point> MotionPath
        {
            get
            {
                return _motionPath;
            }
            set
            {
                if (_motionPath != value)
                {
                    _motionPath = value;
                    OnPropertyChanged("MotionPath");
                }
            }
        }

        private bool _motionEditEnable = false;
        public bool MotionEditEnable
        {
            get
            {
                return _motionEditEnable;
            }
            set
            {
                _motionEditEnable = value;
                OnPropertyChanged("MotionEditEnable");
            }
        }

        public Visibility MaskVisibility
        {
            get
            {
                IPixieHandler pixie = PublicInterface.Get<IPixieHandler>();
                return pixie.IsFeatureEnabled(PixieFeatureenum.MotionTrackWithFilterMask) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #region MotionIDs

        Guid MaskObjectEffectId = new Guid("4364A5B2-385F-4027-83FE-1F25397D38BA");
        Guid MaskObjectEngineId = new Guid("92983A6D-9BFE-4753-96A4-1C7A633EC8EB");
        Guid MaskObjectUIId = new Guid("9134A8C7-5120-442E-8820-F8804FB13F18");
        const int MaskObjectPathID = 5;
        const int MaskObjectIdentifyID = 7;
        const int MaskObjectMosaicStateID = 8;
        const int MaskObjectMosaicAmountID = 9;
        const int MaskObjectBlurStateID = 10;
        const int MaskObjectBlurHorizontalID = 11;
        const int MaskObjectBlurVerticalID = 12;
        const int MaskObjectMosaicDisable = 0;
        const int MaskObjectBlurDisable = 0;
        Guid MaskObjectInvertedEffectId = new Guid("336F9F95-0507-437C-8CCC-B4B533183137");
        Guid MaskObjectInvertedEngineId = new Guid("92983A6D-9BFE-4753-96A4-1C7A633EC8EB");
        Guid MaskObjectInvertedUIId = new Guid("87F2F898-1D6F-427A-A753-00941A0204C6");
        Guid FollowObjectEffectId = new Guid("2E6FAD40-4C5C-4A39-B25C-FFBBE5F19E1F");
        Guid FollowObjectEngineId = new Guid("92983A6D-9BFE-4753-96A4-1C7A633EC8EB");
        Guid FollowObjectUIId = new Guid("19A876E1-6690-4ED2-8FD3-EDC5439D07F6");
        const int FollowObjectPathID = 5;
        const int FollowObjectHorizontalID = 6;
        const int FollowObjectVerticalID = 7;
        const int FollowObjectWidthID = 8;
        const int FollowObjectHeightID = 9;
        Guid FollowObjectSlaveEffectId = new Guid("CA9E3F91-04FA-4931-9175-6C55362CB39B");
        Guid FollowObjectSlaveEngineId = new Guid("92983A6D-9BFE-4753-96A4-1C7A633EC8EB");
        Guid FollowObjectSlaveUIId = new Guid("3A86986E-ABBF-40F9-83EA-438ADE412B16");
        const int FollowObjectSlavePathID = 5;
        const int FollowObjectSlaveIdentifyID = 4;
        const int FollowObjectSlaveHorizontalID = 6;
        const int FollowObjectSlaveVerticalID = 7;
        const int FollowObjectSlaveWidthID = 8;
        const int FollowObjectSlaveHeightID = 9;
        Guid MosaicEffectId = new Guid("9975ce3e-a107-4e5d-ac54-69988b6cfc91");
        Guid MosaicEngineId = new Guid("35e7ce00-4872-11d0-90c3-0000e8ce8250");
        Guid MosaicUIId = new Guid("9975ce3e-a107-4e5d-ac54-69988b6cfc91");
        const int MosaicAmountID = 0;
        Guid BlurEffectId = new Guid("44cb2252-2f35-4489-b769-ca7da0f3699f");
        Guid BlurEngineId = new Guid("35e7ce00-4872-11d0-90c3-0000e8ce8250");
        Guid BlurUIId = new Guid("44cb2252-2f35-4489-b769-ca7da0f3699f");
        const int BlurHorizontalID = 0;
        const int BlurVerticalID = 1;

        #endregion

        private string GetNewTrackName(bool isMask)
        {
            string nameBase = isMask ? StringTranslator.GetString("MotionEditor.MaskEffectName") : StringTranslator.GetString("MotionEditor.FollowEffectName");
            IEffectCollection collection = _parentTrack.GetClipMotionEffects(_clipIndex);
            int nIndex = 1;
            string tempName = nameBase + " (" + nIndex.ToString() + ")";
            if (collection == null)
            {
                return tempName;
            }
            bool bFind = false;
            while (!bFind)
            {
                bFind = true;
                foreach (IEffect e in collection)
                {
                    if (e.Name == tempName)
                    {
                        bFind = false;
                        break;
                    }
                }
                if (!bFind)
                {
                    nIndex++;
                    tempName = nameBase + " (" + nIndex.ToString() + ")";
                }
            }
            return tempName;
        }

        private int GetMotionEffectIndexByID(ID id)
        {
            IEffectCollection iEffectCollection = _parentTrack.GetClipMotionEffects(_clipIndex);
            if (iEffectCollection == null)
            {
                return -1;
            }
            foreach (IEffect effect in iEffectCollection)
            {
                if (effect.ObjectId == id)
                {
                    return iEffectCollection.IndexOf(effect);
                }
            }
            return -1;
        }

        private IEffect GetMotionEffectByIndex(int nIndex)
        {
            int nTemp = 0;
            IEffectCollection iEffectCollection = _parentTrack.GetClipMotionEffects(_clipIndex);
            if (iEffectCollection == null)
            {
                return null;
            }
            foreach (IEffect effect in iEffectCollection)
            {
                IOperatorProperty property = effect.GetProperty(SourceCategories.Video);
                if (property.ID.Operator == MaskObjectEffectId || property.ID.Operator == FollowObjectSlaveEffectId)
                {
                    if (nTemp == nIndex)
                    {
                        return effect;
                    }
                    nTemp++;
                }
            }
            return null;
        }

        private void RefreshDrawingItem()
        {
            MotionPath = null;
            if (CurProperty == null)
            {
                return;
            }
            int nPathID = CurProperty.ID.Operator == MaskObjectEffectId ? MaskObjectPathID : FollowObjectSlavePathID;
            CurProperty.Duration = PreviewModel.Duration;
            TypedParameterValue<byte[]> value = CurProperty.GetParameter(nPathID).InterpolatedValue(PreviewModel.Position) as TypedParameterValue<byte[]>;
            if (value == null)
            {
                return;
            }
            List<Tuple<List<Point>, MTime>> list = ParseMotionTrackInfo(value.Value);
            if (list == null || list.Count == 0)
            {
                return;
            }
            MTime pos = (PreviewModel.Position - TlRecIn) / PreviewModel.Duration;
            if (pos < list[0].Item2 || pos > list[list.Count - 1].Item2)
            {
                return;
            }

            List<Point> pointList = null;
            foreach (Tuple<List<Point>, MTime> item in list)
             {
                if (Math.Abs(item.Item2 * PreviewModel.Duration - PreviewModel.Position + TlRecIn) < PreviewModel.M_Player.FrameLength / 10)
                 {
                     pointList = item.Item1;
                 }
             }

            if (pointList == null)
            {
                return;
            }

            if (CurProperty.ID.Operator == FollowObjectSlaveEffectId)
            {
                String identify = (CurProperty.GetParameter(FollowObjectSlaveIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value;
                uint trackIndex = 0; uint clipIndex = 0;
                if (FindDependTrackClip(identify, ref trackIndex, ref clipIndex))
                {
                    double horizontal = (CurProperty.GetParameter(FollowObjectHorizontalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                    double vertical = (CurProperty.GetParameter(FollowObjectVerticalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                    double width = (CurProperty.GetParameter(FollowObjectWidthID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                    double height = (CurProperty.GetParameter(FollowObjectHeightID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;

                    pointList.Add(new Point(horizontal, vertical));
                    ITrack iTrack = _parentSequence.GetTrack(trackIndex);
                    PipRectParam rectParam = CalcFrameParam(iTrack.GetClip(clipIndex), horizontal, vertical, width, height);
                    pointList.Add(new Point(rectParam.Width / motionCanvasSize.Width, rectParam.Height / motionCanvasSize.Height));
                }
            }

            MotionPath = pointList;
        }

        public void OnMaskObjectItemClicked(DrawTools.ToolType type)
        {
            if (CurProperty == null)
            {
                AddMaskObjectEffect();
            }
            else if (CurProperty.ID.Operator != MaskObjectEffectId)
            {
                AddMaskObjectEffect();
            }
            else if ((CurProperty.GetParameter(MaskObjectPathID).InterpolatedValue(0) as TypedParameterValue<byte[]>).Value.Length > 0)
            {
                AddMaskObjectEffect();
            }
            else
            {
                MotionSelector = type;
            }
        }

        public void OnFollowObjectItemClicked()
        {
            if (CurProperty == null)
            {
                AddFollowObjectSlaveEffect();
            }
            else if (CurProperty.ID.Operator != FollowObjectSlaveEffectId)
            {
                AddFollowObjectSlaveEffect();
            }
            else if ((CurProperty.GetParameter(FollowObjectSlavePathID).InterpolatedValue(0) as TypedParameterValue<byte[]>).Value.Length > 0)
            {
                AddFollowObjectSlaveEffect();
            }
        }

        public void OnMaskObjectPathChanged(List<Point> motionPath, uint type)
        {
            MotionSelector = (DrawTools.ToolType)type;
            MotionPath = motionPath;

            IEffect iEffect = MaskObjectEffectEditor.TheEffect;
            if (iEffect != null)
            {
                IOperatorProperty property = iEffect.GetProperty(SourceCategories.Video);
                List<OperatorInterpolationAction> actions = new List<OperatorInterpolationAction>();
                byte[] motionPathBuffer = (iEffect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectPathID).InterpolatedValue(0) as TypedParameterValue<byte[]>).Value;
                OperatorInterpolationAction pathAction = BuildMotionTrackPathAction(motionPathBuffer, motionPath, PreviewModel.Position, TlRecOut - TlRecIn);
                pathAction.ParameterID = MaskObjectPathID;
                actions.Add(pathAction);

                List<ID> ids = new List<ID>();
                ids.Add(iEffect.ObjectId);
                iEffect = FindDependClipEffect((iEffect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value, MaskObjectInvertedEffectId);
                if (iEffect == null)
                {
                    return;
                }
                ids.Add(iEffect.ObjectId);
                UpdateMotionTrackEffect(ids, actions);
            }
        }

        public Size motionCanvasSize;

        public void OnFollowObjectPathChanged(List<Point> motionPath)
        {
            IEffect iEffect = FollowObjectEffectEditor.TheEffect;
            if (iEffect != null)
            {
                IOperatorProperty property = iEffect.GetProperty(SourceCategories.Video);
                List<OperatorInterpolationAction> actions = new List<OperatorInterpolationAction>();
                byte[] motionPathBuffer = (iEffect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectPathID).InterpolatedValue(0) as TypedParameterValue<byte[]>).Value;
                OperatorInterpolationAction pathAction = BuildMotionTrackPathAction(motionPathBuffer, new List<Point>{ motionPath[0] }, PreviewModel.Position, TlRecOut - TlRecIn);
                pathAction.ParameterID = FollowObjectSlavePathID;
                actions.Add(pathAction);
                String identify = (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectSlaveIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value;
                if (motionPath.Count == 1)
                {
                    uint trackIndex = 0; uint clipIndex = 0;
                    if (FindDependTrackClip(identify, ref trackIndex, ref clipIndex))
                    {
                        double horizontal = (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectHorizontalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                        double vertical = (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectVerticalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                        double width = (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectWidthID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                        double height = (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectHeightID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;

                        motionPath.Add(new Point(horizontal, vertical));
                        ITrack iTrack = _parentSequence.GetTrack(trackIndex);
                        PipRectParam rectParam = CalcFrameParam(iTrack.GetClip(clipIndex), horizontal, vertical, width, height);
                        motionPath.Add(new Point(rectParam.Width / motionCanvasSize.Width, rectParam.Height / motionCanvasSize.Height));
                    }
                }

                List<ID> ids = new List<ID>();
                ids.Add(iEffect.ObjectId);
                iEffect = FindDependClipEffect(identify, FollowObjectEffectId);
                if (iEffect != null)
                {
                    ids.Add(iEffect.ObjectId);
                    if (MotionPath != null && MotionPath.Count == 3)
                    {
                        uint trackIndex = 0; uint clipIndex = 0;
                        if (FindDependTrackClip(identify, ref trackIndex, ref clipIndex))
                        {
                            ITrack iTrack = _parentSequence.GetTrack(trackIndex);
                            PipRectParam rectParam = CalcFrameParam(iTrack.GetClip(clipIndex), 0, 0, 100, 100);
                            OperatorInterpolationAction horizontalAction = new OperatorInterpolationAction();
                            horizontalAction.type = OperatorInterpolationActionType.SetValue;
                            horizontalAction.ParameterID = FollowObjectSlaveHorizontalID;
                            horizontalAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, motionPath[1].X * 100), PreviewModel.Position),
                                                        null, InterpolationTypes.none, TimeInterpolation.linear);
                            OperatorInterpolationAction verticalAction = new OperatorInterpolationAction();
                            verticalAction.type = OperatorInterpolationActionType.SetValue;
                            verticalAction.ParameterID = FollowObjectSlaveVerticalID;
                            verticalAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, motionPath[1].Y * 100), PreviewModel.Position),
                                                        null, InterpolationTypes.none, TimeInterpolation.linear);

                            OperatorInterpolationAction widthAction = new OperatorInterpolationAction();
                            widthAction.type = OperatorInterpolationActionType.SetValue;
                            widthAction.ParameterID = FollowObjectSlaveWidthID;
                            widthAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, motionPath[2].X * motionCanvasSize.Width / rectParam.Width * 100), PreviewModel.Position),
                                                        null, InterpolationTypes.none, TimeInterpolation.linear);
                            OperatorInterpolationAction heightAction = new OperatorInterpolationAction();
                            heightAction.type = OperatorInterpolationActionType.SetValue;
                            heightAction.ParameterID = FollowObjectSlaveHeightID;
                            heightAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, motionPath[2].Y * motionCanvasSize.Height / rectParam.Height * 100), PreviewModel.Position),
                                                        null, InterpolationTypes.none, TimeInterpolation.linear);
                            actions.Add(horizontalAction);
                            actions.Add(verticalAction);
                            actions.Add(widthAction);
                            actions.Add(heightAction);
                        }
                    }
                }
                UpdateFollowObjectPath(actions);
            }

            MotionPath = motionPath;
        }

        PipRectParam CalcFrameParam(IBaseClip baseClip, double horizontal, double vertical, double sizeX, double sizeY)
        {
            PropertyParam param = new PropertyParam();
            param.PositionX = horizontal;
            param.PositionY = vertical;
            param.SizeX = sizeX;
            param.SizeY = sizeY;
            PipRectParam rectParam = new PipRectParam();
            SequenceHelper.CalcuFrame(baseClip, motionCanvasSize.Width, motionCanvasSize.Height, param, ref rectParam);
            return rectParam;
        }

        public void AddMaskObjectEffect()
        {
            ICommandRegistry cmdRegistry = PublicInterface.Get<ICommandRegistry>();
            IMotionTrackEffectAddCommand addTrackEffectCmd = cmdRegistry.CreateCommand<IMotionTrackEffectAddCommand>();
            addTrackEffectCmd.SubCommand = true;
            addTrackEffectCmd.SetSequence(_parentSequence.ObjectId);
            addTrackEffectCmd.SetEditMode(SequenceEditMode.Alternate);
            addTrackEffectCmd.TrackIndex = _parentSequence.GetTrackIndex(_parentTrack);
            addTrackEffectCmd.ClipIndex = _clipIndex;
            addTrackEffectCmd.EffectID = new OperatorId(MaskObjectEngineId, MaskObjectEffectId, MaskObjectUIId);
            addTrackEffectCmd.PresetID = 0;
            addTrackEffectCmd.Simulate = false;
            addTrackEffectCmd.EffectName = GetNewTrackName(true);
            CommandSequence.Execute(addTrackEffectCmd);
        }

        public void AddFollowObjectSlaveEffect()
        {
            ICommandRegistry cmdRegistry = PublicInterface.Get<ICommandRegistry>();
            IMotionTrackEffectAddCommand addTrackEffectCmd = cmdRegistry.CreateCommand<IMotionTrackEffectAddCommand>();
            addTrackEffectCmd.SubCommand = true;
            addTrackEffectCmd.SetSequence(_parentSequence.ObjectId);
            addTrackEffectCmd.SetEditMode(SequenceEditMode.Alternate);
            addTrackEffectCmd.TrackIndex = _parentSequence.GetTrackIndex(_parentTrack);
            addTrackEffectCmd.ClipIndex = _clipIndex;
            addTrackEffectCmd.EffectID = new OperatorId(FollowObjectSlaveEngineId, FollowObjectSlaveEffectId, FollowObjectSlaveUIId);
            addTrackEffectCmd.PresetID = 0;
            addTrackEffectCmd.Simulate = false;
            addTrackEffectCmd.EffectName = GetNewTrackName(false);
            CommandSequence.Execute(addTrackEffectCmd);
        }

        public void AddMotionTrackEffectIdentify(ID effectID, int parameterID, bool subcommand)
        {
            ICommandRegistry cmdRegistry = PublicInterface.Get<ICommandRegistry>();
            IEffectInterpolationCommand command = cmdRegistry.CreateCommand<IEffectInterpolationCommand>();
            command.EffectId = effectID;
            List<OperatorInterpolationAction> actions = new List<OperatorInterpolationAction>();
            OperatorInterpolationAction guidAction = new OperatorInterpolationAction();
            guidAction.type = OperatorInterpolationActionType.SetValue;
            guidAction.ParameterID = parameterID;
            guidAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<String>(ParameterType.String, Guid.NewGuid().ToString()), 0),
                                        null, InterpolationTypes.none, TimeInterpolation.linear);
            actions.Add(guidAction);
            command.SubCommand = subcommand;
            command.SetActions(actions.ToArray());
            command.Simulate = false;
            command.EffectDuration = TlRecOut - TlRecIn;
            command.KeySnapTolerance = VideoFormat.FrameLength;
            CommandSequence.Execute(command);
        }

        public void AddMaskObjectClipSnap()
        {
            IEffect iEffect = MaskObjectEffectEditor.TheEffect;
            if (iEffect == null)
                return;
            
            ICommandRegistry cmdRegistry = PublicInterface.Get<ICommandRegistry>();
            ISequenceAddMotionTrackClipCommand command = cmdRegistry.CreateCommand<ISequenceAddMotionTrackClipCommand>();
            command.TrackIndex = _parentSequence.GetTrackIndex(_parentTrack);
            command.ClipIndex = _clipIndex;
            command.TrackName = iEffect.Name;
            command.Simulate = false;
            command.SetMotionGuid(new Guid((iEffect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value));
            command.SetSequence(_parentSequence.ObjectId);
            command.SetEditMode(SequenceEditMode.Smart);

            List<OperatorInterpolationAction> actions = new List<OperatorInterpolationAction>();
            // add mosaic effect
            int mosaicState = (iEffect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectMosaicStateID).InterpolatedValue(0) as TypedParameterValue<int>).Value;
            OperatorInterpolationAction mosaicAmountAction = new OperatorInterpolationAction();
            mosaicAmountAction.type = OperatorInterpolationActionType.SetValue;
            mosaicAmountAction.ParameterID = MosaicAmountID;
            if (mosaicState == MaskObjectMosaicDisable)
            {
                mosaicAmountAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, 0), PreviewModel.Position),
                                            null, InterpolationTypes.none, TimeInterpolation.linear);
            }
            else
            {
                double amount = (iEffect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectMosaicAmountID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                mosaicAmountAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, amount), PreviewModel.Position),
                                            null, InterpolationTypes.none, TimeInterpolation.linear);
            }
            actions.Add(mosaicAmountAction);
            command.InsertEffect(new OperatorId(MosaicEngineId, MosaicEffectId, MosaicUIId), actions.ToArray());
            // add blur effect
            actions = new List<OperatorInterpolationAction>();
            int blurState = (iEffect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectBlurStateID).InterpolatedValue(0) as TypedParameterValue<int>).Value;
            OperatorInterpolationAction blueHorizontalAction = new OperatorInterpolationAction();
            blueHorizontalAction.type = OperatorInterpolationActionType.SetValue;
            blueHorizontalAction.ParameterID = BlurHorizontalID;
            OperatorInterpolationAction blurVerticalAction = new OperatorInterpolationAction();
            blurVerticalAction.type = OperatorInterpolationActionType.SetValue;
            blurVerticalAction.ParameterID = BlurVerticalID;
            if (blurState == MaskObjectBlurDisable)
            {
                blueHorizontalAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, 0), PreviewModel.Position),
                                            null, InterpolationTypes.none, TimeInterpolation.linear);
                blurVerticalAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, 0), PreviewModel.Position),
                                            null, InterpolationTypes.none, TimeInterpolation.linear);
            }
            else
            {
                double horizontal = (iEffect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectBlurHorizontalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                blueHorizontalAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, horizontal), PreviewModel.Position),
                                            null, InterpolationTypes.none, TimeInterpolation.linear);
                double vertical = (iEffect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectBlurVerticalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value;
                blueHorizontalAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, vertical), PreviewModel.Position),
                                            null, InterpolationTypes.none, TimeInterpolation.linear);

            }
            actions.Add(blueHorizontalAction);
            actions.Add(blurVerticalAction);
            command.InsertEffect(new OperatorId(BlurEngineId, BlurEffectId, BlurUIId), actions.ToArray());
            // add maskObjectInverted effect
            actions = new List<OperatorInterpolationAction>();
            byte[] motionPathBuffer = (iEffect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectPathID).InterpolatedValue(0) as TypedParameterValue<byte[]>).Value;
            OperatorInterpolationAction pathAction = BuildMotionTrackPathAction(motionPathBuffer, new List<Tuple<List<Point>, MTime>>(), GetPosInClipRel(PreviewModel.Position), TlRecOut - TlRecIn);
            pathAction.ParameterID = MaskObjectPathID;
            actions.Add(pathAction);
            command.InsertEffect(new OperatorId(MaskObjectInvertedEngineId, MaskObjectInvertedEffectId, MaskObjectInvertedUIId), actions.ToArray());
            
            command.EffectDuration = _parentTrack.GetDuration(_clipIndex);
            command.KeySnapTolerance = VideoFormat.FrameLength;
            CommandSequence.Execute(command);
        }

        public void ReplaceFollowObjectClip(ID effectId, DbRecord dbRecord, uint trackIndex, uint clipIndex)
        {
            IEffect iEffect = GetMotionEffectByIndex(GetMotionEffectIndexByID(effectId));
            if (iEffect == null)
            {
                return;
            }

            ITrack track = _parentSequence.GetTrack(trackIndex);
            if (track == null)
            {
                return;
            }

            MTime start = 0, end = 0, duration = 0;
            if (!GetMotionTrackPathRange(iEffect, _parentTrack.GetDuration(_clipIndex), ref start, ref end, ref duration))
            {
                return;
            }

            ISequenceInsertCommand insertCommand = CreateCommand<ISequenceInsertCommand>();
            RecordInsertInfo insertInfo = new RecordInsertInfo(dbRecord.Guid, 0, duration);
            insertCommand.AddInsertInfo(insertInfo);
            insertCommand.SetSequence(_parentSequence.ObjectId);
            insertCommand.Is3DEnable = IsFeatureEnabled[PixieFeatureenum.General_3D_Support.ToString()];
            insertCommand.DeduceFormatFromFirstClip = false;
            insertCommand.ReplaceExistingClip = true;
            insertCommand.Position = track.GetRecIn(clipIndex);
            insertCommand.InsertNewTrack = false;
            insertCommand.Categories = SourceCategories.AV;
            insertCommand.TrackIndex = trackIndex;
            insertCommand.SnapFrames = 0;
            insertCommand.AutoSnapFirstClip = false;
            insertCommand.SetEditMode(SequenceEditMode.Overwrite);
            insertCommand.IsSubcommand = false;
            if (ExecuteCommand(insertCommand, false, false))
            {
                Document.Refresh();
                UpdateFollowObjectInput(track);
            }   
        }

        public void AddFollowObjectClip(ID effectId, DbRecord dbRecord)
        {
            IEffect iEffect = GetMotionEffectByIndex(GetMotionEffectIndexByID(effectId));
            if (iEffect == null)
                return;

            MTime start = 0, end = 0, duration = 1;
            GetMotionTrackPathRange(iEffect, _parentTrack.GetDuration(_clipIndex), ref start, ref end, ref duration);

            ICommandRegistry cmdRegistry = PublicInterface.Get<ICommandRegistry>();
            ISequenceAddMotionTrackClipCommand command = cmdRegistry.CreateCommand<ISequenceAddMotionTrackClipCommand>();
            command.TrackIndex = _parentSequence.GetTrackIndex(_parentTrack);
            command.ClipIndex = _clipIndex;
            command.TrackName = iEffect.Name;
            command.Simulate = false;
            command.SetMotionGuid(new Guid((iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectSlaveIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value));
            command.SetInsertInfo(new RecordInsertInfo(dbRecord.Guid, 0, duration));
            command.SetSequence(_parentSequence.ObjectId);
            command.SetEditMode(SequenceEditMode.Smart);
            
            // add followobjectslave effect
            List<OperatorInterpolationAction> actions = new List<OperatorInterpolationAction>();
            OperatorInterpolationAction pathAction = BuildMotionTrackPathAction(
                (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectPathID).InterpolatedValue(0) as TypedParameterValue<byte[]>).Value,
                new List<Tuple<List<Point>, MTime>>(),
                GetPosInClipRel(PreviewModel.Position),
                TlRecOut - TlRecIn,
                true);
            pathAction.ParameterID = FollowObjectSlavePathID;
            OperatorInterpolationAction horizontalAction = new OperatorInterpolationAction();
            horizontalAction.type = OperatorInterpolationActionType.SetValue;
            horizontalAction.ParameterID = FollowObjectSlaveHorizontalID;
            horizontalAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectHorizontalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value), PreviewModel.Position),
                                        null, InterpolationTypes.none, TimeInterpolation.linear);
            OperatorInterpolationAction verticalAction = new OperatorInterpolationAction();
            verticalAction.type = OperatorInterpolationActionType.SetValue;
            verticalAction.ParameterID = FollowObjectSlaveVerticalID;
            verticalAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectVerticalID).InterpolatedValue(0) as TypedParameterValue<Double>).Value), PreviewModel.Position),
                                        null, InterpolationTypes.none, TimeInterpolation.linear);
            OperatorInterpolationAction widthAction = new OperatorInterpolationAction();
            widthAction.type = OperatorInterpolationActionType.SetValue;
            widthAction.ParameterID = FollowObjectSlaveWidthID;
            widthAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectWidthID).InterpolatedValue(0) as TypedParameterValue<Double>).Value), PreviewModel.Position),
                                        null, InterpolationTypes.none, TimeInterpolation.linear);
            OperatorInterpolationAction heightAction = new OperatorInterpolationAction();
            heightAction.type = OperatorInterpolationActionType.SetValue;
            heightAction.ParameterID = FollowObjectSlaveHeightID;
            heightAction.key = new Keyframe(new ParameterPoint(new TypedParameterValue<Double>(ParameterType.Double, (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectHeightID).InterpolatedValue(0) as TypedParameterValue<Double>).Value), PreviewModel.Position),
                                        null, InterpolationTypes.none, TimeInterpolation.linear);
            actions.Add(pathAction);
            actions.Add(horizontalAction);
            actions.Add(verticalAction);
            actions.Add(widthAction);
            actions.Add(heightAction);

            command.RecIn = TlRecIn + start;
            command.InsertEffect(new OperatorId(FollowObjectEngineId, FollowObjectEffectId, FollowObjectUIId), actions.ToArray());
            command.EffectDuration = duration;
            command.KeySnapTolerance = VideoFormat.FrameLength;
            CommandSequence.Execute(command);
        }

        private void UpdateMaskObjectPath(List<Tuple<List<Point>, MTime>> motinPathList, OperatorInterpolationAction? pathAction = null)
        {
            IEffect iEffect = CurEffect;
            if (iEffect != null)
            {
                List<OperatorInterpolationAction> actions = new List<OperatorInterpolationAction>();
                byte[] motionPathBuffer = (iEffect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectPathID).InterpolatedValue(0) as TypedParameterValue<byte[]>).Value;
                if (pathAction == null)
                {
                    pathAction = BuildMotionTrackPathAction(motionPathBuffer, motinPathList, GetPosInClipRel(PreviewModel.Position), TlRecOut - TlRecIn);
                }
                OperatorInterpolationAction action = pathAction.Value;
                action.ParameterID = MaskObjectPathID;
                actions.Add(action);

                List<ID> ids = new List<ID>();
                ids.Add(iEffect.ObjectId);
                iEffect = FindDependClipEffect((iEffect.GetProperty(SourceCategories.Video).GetParameter(MaskObjectIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value, MaskObjectInvertedEffectId);
                if (iEffect == null)
                {
                    return;
                }
                ids.Add(iEffect.ObjectId);
                UpdateMotionTrackEffect(ids, actions);
            }
        }

        private void UpdateFollowObjectPath(List<OperatorInterpolationAction> actions)
        {
            if (actions == null || actions.Count < 1)
            {
                return;
            }
            OperatorInterpolationAction pathAction = actions[0];
            IEffect iEffect = CurEffect;
            ICommandRegistry cmdRegistry = PublicInterface.Get<ICommandRegistry>();
            if (cmdRegistry == null || iEffect == null)
            {
                return;
            }
            TypedParameterValue<byte[]> path = pathAction.key.key.Value as TypedParameterValue<byte[]>;
            MTime start = 0, end = 0, duration = 0;
            if (!GetRangeByPath(path, TlRecOut - TlRecIn, ref start, ref end, ref duration))
            {
                return;
            }

            uint track = 0, clip = 0;
            if (FindDependTrackClip(FollowObjectEffectEditor.TheEffect, ref track, ref clip))
            {
                ISequenceSetClipCommand cmd = cmdRegistry.CreateCommand<ISequenceSetClipCommand>();
                cmd.TrackIndex = track;
                cmd.ClipIndex = clip;
                cmd.RecIn = TlRecIn + start;
                cmd.RecOut = TlRecIn + end;
                cmd.Subcommand = true;
                cmd.SetSequence(this._parentSequence.ObjectId);
                cmd.SetEditMode(SequenceEditMode.Smart);
                cmd.Simulate = false;

                ExecuteCommand(cmd, false, false);
            }

            OperatorInterpolationAction action = pathAction;
            action.ParameterID = FollowObjectSlavePathID;
            actions[0] = action;

            List<ID> ids = new List<ID>();
            ids.Add(iEffect.ObjectId);
            iEffect = FindDependClipEffect((iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectSlaveIdentifyID).InterpolatedValue(0) as TypedParameterValue<String>).Value, FollowObjectEffectId);
            UpdateMotionTrackEffect(ids, actions, iEffect != null);
            ids.Clear();
            if (iEffect != null)
            {
                pathAction = BuildMotionTrackPathAction(path.Value, new List<Tuple<List<Point>, MTime>>(), GetPosInClipRel(PreviewModel.Position), TlRecOut - TlRecIn, true);
                action = pathAction;
                action.ParameterID = FollowObjectSlavePathID;
                actions[0] = action;
                ids.Add(iEffect.ObjectId);
                UpdateMotionTrackEffect(ids, actions);
            }
        }

        private void UpdateFollowObjectPath(List<Tuple<List<Point>, MTime>> motinPathList, OperatorInterpolationAction? pathAction = null)
        {
            IEffect iEffect = CurEffect;
            if (iEffect != null)
            {
                if (pathAction == null)
                {
                    byte[] motionPathBuffer = (iEffect.GetProperty(SourceCategories.Video).GetParameter(FollowObjectSlavePathID).InterpolatedValue(0) as TypedParameterValue<byte[]>).Value;
                    pathAction = BuildMotionTrackPathAction(motionPathBuffer, motinPathList, GetPosInClipRel(PreviewModel.Position), TlRecOut - TlRecIn);
                }
                List<OperatorInterpolationAction> actions = new List<OperatorInterpolationAction>();
                actions.Add(pathAction.Value);
                UpdateFollowObjectPath(actions);
            }
        }

        void UpdateMotionTrackEffect(List<ID> ids, List<OperatorInterpolationAction> actions, bool SubCommand = false)
        {
            ICommandRegistry cmdRegistry = PublicInterface.Get<ICommandRegistry>();
            if (cmdRegistry == null)
            {
                return;
            }

            IMotionTrackEffectUpdateCommand comand = cmdRegistry.CreateCommand<IMotionTrackEffectUpdateCommand>();
            comand.SetSequence(_rootSequence.ObjectId);
            comand.SetEditMode(SequenceEditMode.Smart);
            comand.SetEffectIds(ids.ToArray());
            comand.SetActions(actions.ToArray());
            comand.KeySnapTolerance = VideoFormat.FrameLength;
            comand.Simulate = false;
            comand.EffectDuration = TlRecOut - TlRecIn;
            comand.Subcommand = SubCommand;
            CommandSequence.Execute(comand);
        }

        OperatorInterpolationAction BuildMotionTrackPathAction(byte[] motionPathSequence, List<Point> motionPath, MTime position, MTime duration)
        {
            List<Tuple<List<Point>, MTime>> motionPathList = new List<Tuple<List<Point>, MTime>>();
            motionPathList.Add(new Tuple<List<Point>, MTime>(motionPath, position - TlRecIn));
            return BuildMotionTrackPathAction(motionPathSequence, motionPathList, position, duration);
        }

        OperatorInterpolationAction BuildMotionTrackPathAction(byte[] motionPathSequence, List<Tuple<List<Point>, MTime>> motionPathList, MTime position, MTime duration, bool bMoveToTop = false)
        {
            OperatorInterpolationAction action = new OperatorInterpolationAction();
            action.type = OperatorInterpolationActionType.SetValue;
            action.key = new Keyframe(new ParameterPoint(new TypedParameterValue<byte[]>(ParameterType.Binary, BuildMotionTrackInfo(motionPathSequence, motionPathList, duration, bMoveToTop)), position),
                                            null, InterpolationTypes.none, TimeInterpolation.linear);
            return action;
        }

        List<Tuple<List<Point>, MTime>> ParseMotionTrackInfo(byte[] motionPathSequence)
        {
            List<Tuple<List<Point>, MTime>> motionPathList = new List<Tuple<List<Point>, MTime>>();
            int floatSize = sizeof(float);
            if (motionPathSequence.Length <= 0 || motionPathSequence.Length % floatSize != 0)
                return motionPathList;

            float endFlag = 0xFFFFFFFF;
            int floatCount = motionPathSequence.Length / floatSize;
            Double currentTime = 0.0;
            List<Point> pointList = new List<Point>();
            bool isEnd = true;
            for (int index = 0; index < floatCount; ++index)
            {
                if (isEnd)
                {
                    currentTime = BitConverter.ToSingle(motionPathSequence, index * floatSize);
                    pointList = new List<Point>();
                    isEnd = false;
                }
                else
                {
                    double pointX = BitConverter.ToSingle(motionPathSequence, index * floatSize);
                    if (Math.Abs(pointX - endFlag) < 0.00001)
                    {
                        isEnd = true;
                        motionPathList.Add(new Tuple<List<Point>, MTime>(pointList, currentTime));
                        continue;
                    }

                    index++;
                    if (index >= floatCount)
                        break;
                    double pointY = BitConverter.ToSingle(motionPathSequence, index * floatSize);
                    pointList.Add(new Point(pointX, pointY));
                }
            }

            return motionPathList;
        }

        byte[] BuildMotionTrackInfo(byte[] motionPathSequence, List<Tuple<List<Point>, MTime>> motionPathList, MTime duration, bool bMoveToTp)
        {
            List<Tuple<List<Point>, MTime>> motionInfoList = ParseMotionTrackInfo(motionPathSequence);
            int index1 = 0, index2 = 0;
            while (index1 < motionInfoList.Count && index2 < motionPathList.Count)
            {
                double rate = motionPathList[index2].Item2 / duration;
                if (Math.Abs(motionInfoList[index1].Item2 - rate) < 0.00001)
                {
                    if (motionPathList[index2].Item1.Count > 0)
                    {
                        motionInfoList[index1] = new Tuple<List<Point>, MTime>(motionPathList[index2].Item1, rate);
                    }
                    index2++;
                    continue;
                }
                else if (motionInfoList[index1].Item2 > rate)
                {
                    if (motionPathList[index2].Item1.Count > 0)
                    {
                        motionInfoList.Insert(index1, new Tuple<List<Point>, MTime>(motionPathList[index2].Item1, rate));
                    }
                    index2++;
                    continue;
                }

                index1++;
            }
            for (; index2 < motionPathList.Count; ++index2)
            {
                double rate = motionPathList[index2].Item2 / duration;
                motionInfoList.Add(new Tuple<List<Point>, MTime>(motionPathList[index2].Item1, rate));
            }

            if (bMoveToTp && motionInfoList.Count > 0)
            {
                MTime start = motionInfoList[0].Item2 * duration;
                MTime end = motionInfoList[motionInfoList.Count - 1].Item2 * duration;
                MTime newDuration = end - start + VideoFormat.FrameLength;
                for (int i = 0; i < motionInfoList.Count; i++)
                {
                    motionInfoList[i] = new Tuple<List<Point>, MTime>(motionInfoList[i].Item1, (motionInfoList[i].Item2 * duration - start) / newDuration);
                }
            }

            return BuildMotionTrackInfo(motionInfoList);
        }

        byte[] BuildMotionTrackInfo(List<Tuple<List<Point>, MTime>> motionInfoList)
        {
            float endFlag = 0xFFFFFFFF;
            List<byte> motionPathSequence = new List<byte>();
            for (int index = 0; index < motionInfoList.Count; ++index)
            {
                motionPathSequence.AddRange(BitConverter.GetBytes((float)motionInfoList[index].Item2));
                for (int pointIndex = 0; pointIndex < motionInfoList[index].Item1.Count; ++pointIndex)
                {
                    motionPathSequence.AddRange(BitConverter.GetBytes((float)motionInfoList[index].Item1[pointIndex].X));
                    motionPathSequence.AddRange(BitConverter.GetBytes((float)motionInfoList[index].Item1[pointIndex].Y));
                }
                motionPathSequence.AddRange(BitConverter.GetBytes(endFlag));
            }

            return motionPathSequence.ToArray();
        }

        bool FindDependTrackClip(IEffect effect, ref uint trackIndex, ref uint clipIndex)
        {
            if (effect == null)
            {
                return false;
            }
            IOperatorProperty iop = effect.GetProperty(effect.Categories);
            if (iop == null)
            {
                return false;
            }
            string identify = string.Empty;
            int pathID = 0;
            if (iop.ID.Operator == MaskObjectEffectId)
            {
                pathID = MaskObjectIdentifyID;
            }
            else if (iop.ID.Operator == FollowObjectSlaveEffectId)
            {
                pathID = FollowObjectSlaveIdentifyID;
            }
            else
            {
                return false;
            }
            IParameter p = iop.GetParameter(pathID);
            if (p == null)
            {
                return false;
            }
            TypedParameterValue<string> value = p.InterpolatedValue(0) as TypedParameterValue<string>;
            if (value == null)
            {
                return false;
            }
            identify = value.Value;
            return FindDependTrackClip(identify, ref trackIndex, ref clipIndex);
        }

        bool FindDependTrackClip(String identify, ref uint trackIndex, ref uint clipIndex)
        {
            if (identify == null || identify == string.Empty)
            {
                return false;
            }
            uint trackCount = _parentSequence.GetTrackCount();
            for (uint tIndex = 0; tIndex < trackCount; ++tIndex)
            {
                ITrack track = _parentSequence.GetTrack(tIndex);
                if (track != null)
                {
                    uint clipCount = track.GetClipCount();
                    for (uint cIndex = 0; cIndex < clipCount; ++cIndex)
                    {
                        IMotionTrack motionTrack = track.GetMotionTrack(cIndex);
                        if (motionTrack != null)
                        {
                            if (motionTrack.GUID == new Guid(identify))
                            {
                                trackIndex = tIndex;
                                clipIndex = cIndex;
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        IEffect FindDependMotionTrackEffect(String identify, Guid guid)
        {
            IEffectCollection collection = _parentTrack.GetClipMotionEffects(_clipIndex);
            if (collection != null)
            {
                foreach (IEffect effect in collection)
                {
                    OperatorId operatorId = effect.GetProperty(effect.Categories).ID;
                    if (operatorId.Operator.Equals(guid))
                    {
                        return effect;
                    }
                }
            }
            return null;
        }

        IEffect FindEffectInCollection(IEffectCollection collection, Guid guid)
        {
            if (collection != null)
            {
                foreach (IEffect effect in collection)
                {
                    OperatorId operatorId = effect.GetProperty(effect.Categories).ID;
                    if (operatorId.Operator.Equals(guid))
                    {
                        return effect;
                    }
                }
            }
            return null;
        }

        IEffect FindDependClipEffect(String identify, Guid guid)
        {
            uint trackIndex = 0;
            uint clipIndex = 0;
            if (FindDependTrackClip(identify, ref trackIndex, ref clipIndex))
            {
                ITrack track = _parentSequence.GetTrack(trackIndex);
                if (track != null)
                {
                    IEffectCollection collection = track.GetClipEffects(clipIndex);
                    IEffect effect = FindEffectInCollection(collection, guid);
                    if (effect != null)
                    {
                        return effect;
                    }
                    collection = track.GetClipMotionEffects(clipIndex);
                    return FindEffectInCollection(collection, guid);
                }
            }

            return null;
        }

        IEffect FindMotionTrackEffectByIdentify(String identify)
        {
            IEffectCollection collection = _parentTrack.GetClipMotionEffects(_clipIndex);
            if (collection == null)
            {
                return null;
            }
            foreach (IEffect effect in collection)
            {
                string id = string.Empty;
                if (GetEffectIdentify(effect, ref id))
                {
                    if (id == identify)
                    {
                        return effect;
                    }
                }
            }
            return null;
        }

        bool GetMotionTrackPathRange(IEffect effect, MTime effectDuration, ref MTime start, ref MTime end, ref MTime duration)
        {
            IOperatorProperty iop = effect.GetProperty(effect.Categories);
            if (iop == null)
            {
                return false;
            }
            int nPathID = 0;
            if (iop.ID.Operator == MaskObjectEffectId)
            {
                nPathID = MaskObjectPathID;
            }
            else if (iop.ID.Operator == FollowObjectSlaveEffectId)
            {
                nPathID = FollowObjectSlavePathID;
            }
            else
            {
                return false;
            }
            IParameter ip = iop.GetParameter(nPathID);
            if (ip == null)
            {
                return false;
            }
            TypedParameterValue<byte[]> value = ip.InterpolatedValue(0) as TypedParameterValue<byte[]>;
            if (value == null)
            {
                return false;
            }
            return GetRangeByPath(value, effectDuration, ref start, ref end, ref duration);
        }

        bool GetRangeByPath(TypedParameterValue<byte[]> path, MTime effectDuration, ref MTime start, ref MTime end, ref MTime duration)
        {
            byte[] motionPath = path.Value;
            List<Tuple<List<Point>, MTime>> motionInfoList = ParseMotionTrackInfo(motionPath);
            if (motionInfoList == null || motionInfoList.Count == 0)
            {
                return false;
            }
            start = motionInfoList[0].Item2;
            end = motionInfoList[motionInfoList.Count - 1].Item2;
            start = (start * effectDuration).Align(VideoFormat.FrameLength);
            end = (end * effectDuration).Align(VideoFormat.FrameLength) + VideoFormat.FrameLength;
            duration = end - start;
            return true;
        }

        public void DeleteCurEffect()
        {
            if (Inspector_SelectedIndex < 0 || Inspector_SelectedIndex >= _inputPropList.Count)
            {
                return;
            }
            InputValueproperty item = _inputPropList[Inspector_SelectedIndex];
            PreviewDeleteEffect(item.ID);
            DeleteEffectByIdentify(item.ID);
        }

        public void DeleteEffectByIdentify(string identify)
        {
            IEffectCollection c = _parentTrack.GetClipMotionEffects(_clipIndex);
            if (c == null)
            {
                return;
            }
            string temp = null;
            int nPos = -1;
            foreach (var obj in c)
            {
                if (GetEffectIdentify(obj, ref temp))
                {
                    if (temp == identify)
                    {
                        nPos = c.IndexOf(obj);
                        break;
                    }
                }
            }
            if (nPos == -1)
            {
                return;
            }
            EffectCollectionHandler.RemoveMotionTrack(_parentSequence, _parentTrack, _parentTrack.GetClip(_clipIndex), nPos, CommandSequence);
        }

        private void OnDeleteEffect(ID objectId)
        {
            for (int i = 0; i < _inputPropList.Count; i++)
            {
                if (_inputPropList[i].ObjectId == objectId)
                {
                    _inputList.RemoveAt(i);
                    _inputPropList.RemoveAt(i);
                    break;
                }
            }
        }

        public void PreviewDeleteEffect(string identify)
        {
            uint nTrackIndex = 0, nClipIndex = 0;
            for (int i = 0; i < _inputPropList.Count; i++)
            {
                if (_inputPropList[i].ID == identify)
                {
                    _inputList.RemoveAt(i);
                    _inputPropList.RemoveAt(i);
                    MotionBtnVis = DrawTools.ToolType.None;
                    break;
                }
            }
            if (FindDependTrackClip(identify, ref nTrackIndex, ref nClipIndex))
            {
                DeleteTrackByIndex(nTrackIndex);
                Document.Refresh();
            }
        }

        private void OnDeleteEffectPreview(object sender, ref int nPos)
        {
            FxStackEffectItem item = sender as FxStackEffectItem;
            if (item == null)
            {
                return;
            }
            IEffect effect = _document.GetObject(item.ObjectID).As<IEffect>();
            if (effect == null)
            {
                return;
            }
            nPos = GetMotionEffectIndexByID(item.ObjectID);
            string identify = null;
            if (!GetEffectIdentify(effect, ref identify))
            {
                return;
            }
            PreviewDeleteEffect(identify);
        }

        private void DeleteTrackByIndex(uint nIndex)
        {
            ISequenceDeleteTracksCommand command = CreateCommand<ISequenceDeleteTracksCommand>();
            if (command == null)
            {
                return;
            }
            command.SubCommand = true;
            command.AddTrackIndex(nIndex);
            command.SetSequence(_parentSequence.ObjectId);
            command.SetEditMode(SequenceEditMode.Ripple);
            CommandSequence.Execute(command);
        }

        private bool GetEffectIdentify(IEffect iE, ref string identify)
        {
            int identifyID = 0;
            IOperatorProperty iop = iE.GetProperty(iE.Categories);
            if (iop == null)
            {
                return false;
            }
            if (iop.ID.Operator == MaskObjectEffectId)
            {
                identifyID = MaskObjectIdentifyID;
            }
            else if (iop.ID.Operator == FollowObjectSlaveEffectId)
            {
                identifyID = FollowObjectSlaveIdentifyID;
            }
            else
            {
                return false;
            }
            IParameter p = iop.GetParameter(identifyID);
            if (p == null)
            {
                return false;
            }
            TypedParameterValue<string> value = p.InterpolatedValue(0) as TypedParameterValue<string>;
            if (value == null)
            {
                return false;
            }
            identify = value.Value;
            return true;
        }

        public struct TrackPoint
        {
            public float x;
            public float y;
        }

        public void MotionTrackObjectAnalysis(MotionParseMode motionParseMode)
        {
            if (MotionPath == null || MotionPath.Count <= 0)
                return;

            List<Point> pointList = new List<Point>();
            if (CurProperty.ID.Operator == FollowObjectSlaveEffectId)
            {
                pointList.Add(MotionPath[0]);
            }
            else
            {
                pointList = MotionPath;
            }
            List<Tuple<List<Point>, MTime>> motionPathList = ObjectObjectAnalysis(_parentTrack, _parentTrack.GetClip(_clipIndex).As<IBaseClip>(), motionParseMode, pointList);
            if (CurProperty == null)
            {
                return;
            }
            if (CurProperty.ID.Operator == MaskObjectEffectId)
            {
                UpdateMaskObjectPath(motionPathList);
            }
            else if (CurProperty.ID.Operator == FollowObjectSlaveEffectId)
            {
                UpdateFollowObjectPath(motionPathList);
            }
            else
            {
                return;
            }
            if (motionPathList == null || motionPathList.Count == 0)
            {
                _pathItems.Clear();
                return;
            }
}

        private List<Tuple<List<Point>, MTime>> ObjectObjectAnalysis(ITrack track, IBaseClip clip, MotionParseMode motionParseMode, List<Point> pointList)
        {
            List<Tuple<List<Point>, MTime>> motionPathList = new List<Tuple<List<Point>, MTime>>();
            if (pointList == null || pointList.Count <= 0)
            {
                return motionPathList;
            }

            uint sampleCount = 0;
            switch (motionParseMode)
            {
                case MotionParseMode.Backward:
                    {
                        sampleCount = uint.MaxValue;
                    }
                    break;
                case MotionParseMode.BackwareStep:
                    {
                        if ((PreviewModel.Position - VideoFormat.FrameLength).Aligned(VideoFormat.FrameLength) < _previewModel.M_Transport.Range_Start)
                            return motionPathList;

                        sampleCount = 1;
                    }
                    break;
                case MotionParseMode.Both:
                    {
                        sampleCount = uint.MaxValue;
                    }
                    break;
                case MotionParseMode.ForwardStep:
                    {
                        if ((PreviewModel.Position + VideoFormat.FrameLength).Aligned(VideoFormat.FrameLength) > _previewModel.M_Transport.Range_End)
                            return motionPathList;

                        sampleCount = 1;
                    }
                    break;
                case MotionParseMode.Forward:
                    {
                        sampleCount = uint.MaxValue;
                    }
                    break;
            }

            ProgressWindow pw = new ProgressWindow(StringTranslator.GetString("ProgressWindow.MotionTrackTitle"), StringTranslator.GetString("ProgressWindow.MotionTrackTaskDescription"));
            Dispatcher dispatcher = pw.Dispatcher;
            bool isClosed = false;

            MTime tStart, tEnd;
            tStart = track.GetClipTime(track.GetClipIndex(clip.ObjectId), 0);
            tEnd = track.GetClipTime(track.GetClipIndex(clip.ObjectId), 1);

            Thread motionParseThrad = new Thread(() =>
            {
                IBasePlayerModel _M_Player = ClassTypeImplementingInterface.CreateInstance<IBasePlayerModel>();
                if (_M_Player == null)
                {
                    return;
                }

                _M_Player.ForceCPUDisplay = true;
                _M_Player.PlaybackDisplayMode = PlaybackDisplayMode.PS_Stereo_Force2D;
                _M_Player.PlaybackQualityMode = PlaybackQuality.PQ_Render;
                dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send, new Action(
                () =>
                {
                    _M_Player.SetClip(clip);
                }));

                _M_Player.Player.Seek(GetPosInClip(PreviewModel.Position), false, 0, true);
                InlaySample sample = InlayConnector.GetCurrentMediaSample(_M_Player.hWnd);
                while (sample == null)
                {
                    sample = InlayConnector.GetCurrentMediaSample(_M_Player.hWnd);
                }

                uint width = sample.GetWidth();
                uint height = sample.GetHeight();
                uint displayTickCount = sample.GetDisplayTickCount();

                InitTrackingManager((int)width, (int)height, (int)width * 4);

                byte[] rgbaArray = ConvertSampleToRGBA(sample);
                List<TrackPoint> trackPoints = new List<TrackPoint>();
                for (int index = 0; index < pointList.Count; ++index)
                {
                    TrackPoint point = new TrackPoint();
                    point.x = (float)pointList[index].X;
                    point.y = (float)pointList[index].Y;
                    trackPoints.Add(point);
                }
                StartTracker(rgbaArray, ref trackPoints.ToArray()[0], pointList.Count);

                if (motionParseMode == MotionParseMode.Backward || motionParseMode == MotionParseMode.BackwareStep || motionParseMode == MotionParseMode.Both)
                {
                    for (int step = 0; step <= sampleCount && !isClosed; step++)
                    {
                        MTime newPos = new MTime((double)-step * (double)VideoFormat.FrameLength) + GetPosInClip(PreviewModel.Position);
                        newPos = newPos.Aligned(VideoFormat.FrameLength);
                        if (newPos < GetPosInClip(tStart))
                            break;

                        List<Point> motionPath = ParseSample(_M_Player, newPos, ref displayTickCount, pointList.Count);
                        if (motionPath.Count <= 0)
                            break;

                        motionPathList.Insert(0, new Tuple<List<Point>, MTime>(motionPath, GetPosInClipRel(PreviewModel.Position) - GetPosInClip(PreviewModel.Position) + newPos));
                    }

                    if (motionParseMode == MotionParseMode.Both)
                    {
                        _M_Player.Player.Seek(GetPosInClip(PreviewModel.Position), false, 0, true);
                        sample = InlayConnector.GetCurrentMediaSample(_M_Player.hWnd);
                        while (sample == null || sample.GetDisplayTickCount() == displayTickCount)
                        {
                            if (sample != null)
                                sample.Release();
                            sample = InlayConnector.GetCurrentMediaSample(_M_Player.hWnd);
                        }
                        displayTickCount = sample.GetDisplayTickCount();
                        rgbaArray = ConvertSampleToRGBA(sample);
                        StartTracker(rgbaArray, ref trackPoints.ToArray()[0], pointList.Count);
                    }
                }

                if (motionParseMode == MotionParseMode.Forward || motionParseMode == MotionParseMode.ForwardStep || motionParseMode == MotionParseMode.Both)
                {
                    for (int step = 0; step <= sampleCount && !isClosed; step++)
                    {
                        MTime newPos = new MTime((double)step * (double)VideoFormat.FrameLength) + GetPosInClip(PreviewModel.Position);
                        newPos = newPos.Aligned(VideoFormat.FrameLength);
                        if (newPos > GetPosInClip(tEnd))
                            break;

                        List<Point> motionPath = ParseSample(_M_Player, newPos, ref displayTickCount, pointList.Count);
                        if (motionPath.Count <= 0)
                            break;

                        motionPathList.Add(new Tuple<List<Point>, MTime>(motionPath, newPos - GetPosInClip(PreviewModel.Position) + GetPosInClipRel(PreviewModel.Position)));
                    }
                }

                if (!isClosed)
                {
                    dispatcher.Invoke(new Action(delegate()
                    {
                        pw.Close();
                    }));
                }
            });
            pw.Cancel += delegate(object s, EventArgs args) { isClosed = true; };
            pw.Load += delegate(object s, EventArgs args)
            {
                motionParseThrad.Start();
            };
            Window desktop = Application.Current.MainWindow;
            pw.Owner = desktop;
            pw.ShowDialog();
            motionParseThrad.Join();

            switch (motionParseMode)
            {
                case MotionParseMode.BackwareStep:
                    {
                        _previewModel.M_Transport.Seek(PreviewModel.Position - VideoFormat.FrameLength, 0, false);
                    }
                    break;
                case MotionParseMode.ForwardStep:
                    {
                        _previewModel.M_Transport.Seek(PreviewModel.Position + VideoFormat.FrameLength, 0, false);
                    }
                    break;
            }

            return motionPathList;
        }

        private byte[] ConvertSampleToRGBA(InlaySample sample)
        {
            uint width = sample.GetWidth();
            uint height = sample.GetHeight();
            IopBitmap iopBitmap = new IopBitmap((int)width, (int)height, false);
            sample.ReadBuffer(iopBitmap.RawData, iopBitmap.Width, iopBitmap.Height, iopBitmap.Stride);
            sample.Release();
            byte[] rgbaArray = new byte[iopBitmap.Height * iopBitmap.Width * 4];
            Marshal.Copy(iopBitmap.RawData, rgbaArray, 0, iopBitmap.Height * iopBitmap.Width * 4);
            iopBitmap.Dispose();
            return rgbaArray;
        }

        private List<Point> ParseSample(IBasePlayerModel _M_Player, MTime position, ref uint displayTickCount, int pointCount)
        {
            _M_Player.Player.Seek(position, false, 0, false);
            InlaySample sample = InlayConnector.GetCurrentMediaSample(_M_Player.hWnd);
            while (sample == null || sample.GetDisplayTickCount() == displayTickCount)
            {
                if (sample != null)
                    sample.Release();
                sample = InlayConnector.GetCurrentMediaSample(_M_Player.hWnd);
            }
            displayTickCount = sample.GetDisplayTickCount();
            byte[] rgbaArray = ConvertSampleToRGBA(sample);

            TrackPoint[] pts = new TrackPoint[pointCount];
            int ptsCount = pointCount;
            GoTracker(rgbaArray, ref pts[0], ref ptsCount);
            if (ptsCount <= 0)
                return new List<Point>();

            List<Point> motionPath = new List<Point>();
            for (int index = 0; index < ptsCount; ++index)
            {
                motionPath.Add(new Point(pts[index].x, pts[index].y));
            }
            return motionPath;
        }

        #region dllimport

        [DllImport("TrackingManager.dll", EntryPoint = "InitTrackingManager", CharSet = CharSet.Unicode)]
        public static extern void InitTrackingManager(int nWidth, int nHeight, int nPitch);

        [DllImport("TrackingManager.dll", EntryPoint = "StartTracker", CharSet = CharSet.Unicode)]
        public static extern void StartTracker(byte[] pBuffer, ref TrackPoint pts, int size);

        [DllImport("TrackingManager.dll", EntryPoint = "GoTracker", CharSet = CharSet.Unicode)]
        public static extern void GoTracker(byte[] pBuffer, ref TrackPoint pts, ref int size);

        #endregion
    }
}
