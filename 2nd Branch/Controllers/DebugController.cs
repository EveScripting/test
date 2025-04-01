/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 26.06.2016
 * Time: 18:31
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py;
//using SC::SharedComponents.SQLite;
//using ServiceStack;
//using ServiceStack.OrmLite;
//using ServiceStack.Text;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of ExampleController.
    /// </summary>
    public class DebugController : BaseController
    {
        #region Constructors

        public DebugController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
            Form = new DebugControllerForm(this);
        }

        #endregion Constructors

        #region Methods

        public override void DoWork()
        {
            try
            {
                //if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                //var entsx = ESCache.Instance.EntitiesNotSelf.Where(e => e.IsPlayer && e.Distance < 1000000);

                //foreach(var ent in entsx)
                //{
                //    Log($"CorpId {ent.DirectEntity.CorpId} Standing {ESCache.Instance.DirectEve.Standings.GetCorporationRelationship(ent.DirectEntity.CorpId)}");
                //}

                //    return;

                //var items = ESCache.Instance.DirectEve.GetItemHangar().Items;
                //var totalVolume = items.Sum(i => i.Quantity * i.Volume);

                //foreach (var item in items)
                //{
                //    Log($"{item.TypeName} {item.TypeId} {item.GroupId} Quant {item.Quantity} Vol {item.Volume} TotalVol {item.Quantity * item.Volume}");
                //}

                //Log($"Totalvol {totalVolume}");

                //var iters = WCFClient.Instance.GetPipeProxy.GetDumpLootIterations(ESCache.Instance.EveAccount.CharacterName);
                //Log($"DumpLootIterations {iters}");
                //WCFClient.Instance.GetPipeProxy.IncreaseDumpLootIterations(ESCache.Instance.EveAccount.CharacterName);
                //if (iters == 11)
                //    WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.DumpLootTimestamp), DateTime.UtcNow.AddHours(-25));

                //var loot2Dump = ESCache.Instance.UnloadLoot.LootItemsInItemHangar();
                //var totalLoot2DumpVolume = loot2Dump.Sum(i => i.Quantity * i.Volume);
                //Log($"{totalLoot2DumpVolume}");

                //ESCache.Instance.DirectEve.BookmarkCurrentLocation("x");

                //ESCache.Instance.DirectEve.CreatePersonalBookmarkFolder("temp");

                //Log($"{ESCache.Instance.DirectEve.BookmarkFolders.Count}");
                //if (ESCache.Instance.DirectEve.BookmarkFolders.Any())
                //{

                //    var folder = ESCache.Instance.DirectEve.BookmarkFolders.FirstOrDefault();
                //    Log(folder.PyObject.LogObject());
                //}

                //foreach (var k in ESCache.Instance.DirectEve.BookmarkFolders)
                //{
                //    Log($"{k.IsActive} {k.IsPersonal}");
                //}

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            IsPaused = true;
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods

    }
}