using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using System.Collections.Immutable;
using VRage.Library.Collections;

namespace IngameScript {
    /// A seeker head utilizing a camera to raycast to accquire and track other ships
    class Seeker {
        /// Different types of scan patterns that can be utilized to provide target accquisition and tracking data
        public enum ScanMode {
            /// Provides only target accquisition and ranging data while also scanning for new contacts
            ///
            /// Searches in a starburst pattern across the camera's FOV
            RangeWhileScanStarburst,
            
            /// Provides STT-lock functionality, foregoing scan for new contacts in order to accurately range a selected target
            SingleTargetTrackPredictive,
        }
        
        public ScanMode Mode = ScanMode.RangeWhileScanStarburst;
        public double ScanRange = 100;
        public readonly Process<Nil> Seek;
        public bool Locked {
            get { return _lock != null; }
        }

        IMyGridProgramRuntimeInfo _rt;
        IMyCameraBlock _cam;
        /// A map of entity IDs to their corresponding contact
        Dictionary<long, Contact> _lock = new Dictionary<long, Contact>();
        
        /// A contact accquired by a ranging raycast with contact time and position data
        struct Contact {
            /// Position and velocity data acquired by a ranging scan
            public MyDetectedEntityInfo body;
            /// Last ping by a raycast
            public float time;
        }
        
        /// The entity to track with a STT search pattern
        long _trackedEntity = -1;
        /// Progress through the current search pattern, 0 to 1
        float _patternProgress;

        public Seeker(IMyGridTerminalSystem gts, IMyProgrammableBlock me, IMyGridProgramRuntimeInfo rt) {
            List<IMyCameraBlock> cams = new List<IMyCameraBlock>();
            gts.GetBlocksOfType(
                cams,
                (cam) => MyIni.HasSection(cam.CustomData, "seeker") && cam.IsSameConstructAs(me)
            );

            if(cams.Count != 1) {
                Log.Panic("Must have exactly one block with a [seeker] attribute in custom data");
            }
            _cam = cams.First();

            _lock = null;
            _patternProgress = 0f;
            _rt = rt;

            Seek = new MethodProcess(SeekProc);
        }

        private IEnumerator<Nil> SeekProc() {
            for(;;) {
                float time = (float)_rt.TimeSinceLastRun.TotalSeconds;
                _patternProgress += time;
                _patternProgress = (_patternProgress > 1) ? 0 : _patternProgress;

                switch(Mode) {
                    case ScanMode.RangeWhileScanStarburst: {
                        float len = _patternProgress * (float)Math.Tau;
                        float angle = len / 30f;
                        len = _cam.RaycastConeLimit * (float)Math.Sin(len);

                        float pitch = len * (float)Math.Sin(angle);
                        float yaw = len * (float)Math.Cos(angle);

                        var info = _cam.Raycast(ScanRange, pitch, yaw);
                        if(!info.IsEmpty()) {
                            Log.Put($"Got contact when scanning {pitch}, {yaw} - {info.EntityId} @ {info.Position}");
                            long id = info.EntityId;
                            Contact ping;
                            
                            ping.body = info;
                            ping.time = (float)_rt.TimeSinceLastRun.TotalSeconds;
                            _lock.Add(id, ping);    
                        }
                    } break;
                }

                yield return Nil._;
            }
        }
    }
}
