// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;

using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectSkills : DirectObject
    {
        #region Fields

        private List<DirectInvType> _allSkills;
        private TimeSpan? _maxQueueLength;
        private List<DirectSkill> _mySkillQueue;
        private List<DirectSkill> _mySkills;

        private TimeSpan? _skillQueueLength;
        private int? _numberOfSkillsInQueue;
        private double? _totalSkillPoints;

        #endregion Fields

        #region Constructors

        internal DirectSkills(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     Return all skills in the game
        /// </summary>
        public List<DirectInvType> AllSkills
        {
            get
            {
                if (_allSkills == null)
                {
                    _allSkills = new List<DirectInvType>();
                    Dictionary<PyObject, PyObject> pySkills = DirectEve.GetLocalSvc("skills").Call("GetAllSkills").ToDictionary();
                    foreach (KeyValuePair<PyObject, PyObject> s in pySkills)
                    {
                        DirectInvType skill = new DirectInvType(DirectEve)
                        {
                            TypeId = (int)s.Value.Attribute("typeID")
                        };

                        _allSkills.Add(skill);
                    }
                }

                return _allSkills;
            }
        }

        /// <summary>
        ///     Returns if MySkills is valid
        /// </summary>
        public bool AreMySkillsReady => DirectEve.GetLocalSvc("skills", false, false).Attribute("myskills").IsValid;

        /// <summary>
        ///     Is the skill data ready?
        /// </summary>
        public bool IsReady => DirectEve.GetLocalSvc("skillqueue").IsValid && DirectEve.GetLocalSvc("skills").IsValid;

        /// <summary>
        ///     Return the skill queue length
        /// </summary>
        public TimeSpan MaxQueueLength => (TimeSpan) (_maxQueueLength ?? (_maxQueueLength =
                                                          new TimeSpan((long) DirectEve.GetLocalSvc("skillqueue").Call("GetMaxSkillQueueLimitLength"))));

        /// <summary>
        ///     Return the current skill queue
        /// </summary>
        /// // [11:48:46] [MySkillQueue]
        /// <KeyVal: {'trainingStartSP' : 2026, 'queuePosition' : 0, 'trainingTypeID' : 3454, 'trainingDestinationSP'
        ///     : 7072, 'trainingEndTime' : 131572268400000000 L, 'trainingStartTime' : 131572066560000000 L, 'trainingToLevel' : 2}>
        public List<DirectSkill> MySkillQueue
        {
            get
            {
                if (_mySkillQueue == null)
                {
                    List<PyObject> pySkills = DirectEve.GetLocalSvc("skillqueue").Attribute("skillQueue").ToList();

                    _mySkillQueue = new List<DirectSkill>();
                    foreach (PyObject s in pySkills)
                    {
                        DirectSkill skill = new DirectSkill(DirectEve, PySharp.PyZero)
                        {
                            TypeId = s.Attribute("trainingTypeID").ToInt(),
                            Level = s.Attribute("trainingToLevel").ToInt(),
                            TrainingStartSP = s.Attribute("trainingStartSP").ToInt(),
                            QueuePosition = s.Attribute("queuePosition").ToInt(),
                            TrainingEndTime = s.Attribute("trainingEndTime").ToDateTime(),
                            TrainingStartTime = s.Attribute("trainingStartTime").ToDateTime(),
                            TrainingToLevel = s.Attribute("trainingToLevel").ToInt(),
                        };

                        _mySkillQueue.Add(skill);
                    }
                }
                return _mySkillQueue;
            }
        }

        /// <summary>
        ///     Return my skills
        /// </summary>
        public List<DirectSkill> MySkills
        {
            get
            {
                if (_mySkills == null)
                    _mySkills = DirectEve.GetLocalSvc("skills").Attribute("myskills").ToDictionary().Select(s => new DirectSkill(DirectEve, s.Value)).ToList();

                return _mySkills;
            }
        }

        public bool SkillInTraining => !DirectEve.GetLocalSvc("skillqueue").Call("SkillInTraining").IsNone;

        /// <summary>
        ///     Return the skill queue length
        /// </summary>
        public TimeSpan SkillQueueLength => (TimeSpan) (_skillQueueLength ?? (_skillQueueLength =
                                                            new TimeSpan((long) DirectEve.GetLocalSvc("skillqueue").Call("GetTrainingLengthOfQueue"))));
        public int GetNumberOfSkillsInQueue => (int)(_numberOfSkillsInQueue ?? (_numberOfSkillsInQueue = (int)DirectEve.GetLocalSvc("skillqueue").Call("GetNumberOfSkillsInQueue")));

        public double TotalSkillPoints => (double)(_totalSkillPoints ?? (_totalSkillPoints = (double)DirectEve.GetLocalSvc("skills").Call("GetSkillPoints")));
        public string TotalSkillPointsAsString
        {
            get
            {
                return TotalSkillPoints.ToString("N0");
            }
        }

        //Alpha accounts are limits to 24 hours of skill queue
        //and Omega (plexed) accounts are probably signing in once a day.
        //this should probably be used to determine when the queue is full!
        public bool SkillQueueHasRoomForMoreSkills
        {
            get
            {
                if (DirectEve.Skills.MySkillQueue.Count == 0)
                    return true;

                if (TimeSpan.FromHours(24) >= DirectEve.Skills.SkillQueueLength)
                    return true;

                return false;
            }
        }

        /// <summary>
        ///     Probably worth to have the skill queue wnd open while calling
        /// </summary>
        /// <returns></returns>
        public bool AbortTrain()
        {
            return SkillInTraining && DirectEve.ThreadedCall(DirectEve.GetLocalSvc("skills").Attribute("AbortTrain"));
        }

        public bool StartTrain()
        {
            return !SkillInTraining && DirectEve.ThreadedCall(DirectEve.GetLocalSvc("skillqueue").Attribute("CommitTransaction"))
                   && DirectEve.ThreadedCall(DirectEve.GetLocalSvc("skillqueue").Attribute("BeginTransaction"));
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Add a skill to the end of the queue
        /// </summary>
        /// <param name="skill"></param>
        /// <returns></returns>
        public bool AddSkillToEndOfQueue(int typeId)
        {
            if (!AreMySkillsReady)
                return false;

            if (!CanTrainSkill(typeId))
                return false;

            // Assume level 0
            int currentLevel = 0;

            // Get the skill from 'MySkills'
            DirectSkill mySkill = MySkills.Find(s => s.TypeId == typeId);
            if (mySkill != null)
                currentLevel = mySkill.Level;

            // Assume 1 level higher then current
            int nextLevel = currentLevel + 1;

            // Check if the skill is already in the queue
            // due to the OrderByDescending on Level, this will
            // result in the highest version of this skill in the queue
            mySkill = MySkillQueue.OrderByDescending(s => s.Level).FirstOrDefault(s => s.TypeId == typeId);
            if (mySkill != null)
                nextLevel = mySkill.Level + 1;

            if (nextLevel > 5)
                return false;

            if (nextLevel > MaxCloneSkillLevel(typeId) || IsRestricted(typeId))
                return false;

            return DirectEve.ThreadedLocalSvcCall("skillqueue", "AddSkillToEnd", typeId, currentLevel, nextLevel);
        }

        /// <summary>
        ///     Add a skill to the start of the queue
        /// </summary>
        /// <param name="skill"></param>
        /// <returns></returns>
        public bool AddSkillToFrontOfQueue(int typeId)
        {
            if (!AreMySkillsReady)
                return false;

            if (!CanTrainSkill(typeId))
                return false;

            // Assume level 1
            int toLevel = 1;

            // Get the skill from 'MySkills'
            DirectSkill mySkill = MySkills.Find(s => s.TypeId == typeId);
            if (mySkill != null)
                toLevel = mySkill.Level + 1;

            if (toLevel > 5)
                return false;

            if (toLevel > MaxCloneSkillLevel(typeId) || IsRestricted(typeId))
                return false;

            return DirectEve.ThreadedLocalSvcCall("skillqueue", "TrainSkillNow", typeId, toLevel);
        }

        public bool CanTrainSkill(int typeId)
        {
            if (!AreMySkillsReady)
                return false;

            DirectInvType inv = DirectEve.GetInvType(typeId);
            if (inv == null)
                return false;

            if (GetRequiredSkillsForType(typeId).Count > 0)
                return false;

            return true;
        }

        /// <summary>
        ///     Returns the requirements for the given skill. Already trained skills are included
        ///     Only call after AreMySkillsReady == true, else the result is unspecified ( if exclude == true )
        /// </summary>
        /// <returns></returns>
        public List<Tuple<int, int>> GetRequiredSkillsForType(int typeId, bool excludeMySkills = true)
        {
            List<Tuple<int, int>> ret = new List<Tuple<int, int>>();

            if (!AreMySkillsReady)
                return ret;

            List<DirectSkill> mySkills = MySkills;
            Dictionary<int, PyObject> dict = DirectEve.GetLocalSvc("skills").Call("GetRequiredSkillsRecursive", typeId).ToDictionary<int>();
            foreach (KeyValuePair<int, PyObject> kv in dict)
            {
                int key = kv.Key;
                int val = kv.Value.ToInt();
                if (excludeMySkills && mySkills.Any(s => s.TypeId == key && s.Level >= val))
                    continue;

                ret.Add(new Tuple<int, int>(key, val));
            }

            return ret;
        }

        public bool IsRestricted(int typeId)
        {
            DirectInvType inv = DirectEve.GetInvType(typeId);
            if (inv == null)
                return true;
            return DirectEve.GetLocalSvc("cloneGradeSvc").Call("IsRestricted", typeId).ToBool();
        }

        public int MaxCloneSkillLevel(int typeId)
        {
            DirectInvType inv = DirectEve.GetInvType(typeId);
            if (inv == null)
                return 0;
            return DirectEve.GetLocalSvc("cloneGradeSvc").Call("GetMaxSkillLevel", typeId).ToInt();
        }

        /// <summary>
        ///     Refresh MySkills
        /// </summary>
        /// <returns></returns>
        public void RefreshMySkills()
        {
            if (!AreMySkillsReady)
                DirectEve.ThreadedLocalSvcCall("skills", "RefreshMySkills");
        }

        #endregion Methods
    }
}