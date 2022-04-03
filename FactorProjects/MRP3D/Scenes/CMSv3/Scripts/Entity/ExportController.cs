using System;
using TMPro;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ExportController : MonoBehaviour, IExchangeable, ILinkedToPlane, IResetable, IManualInit<ExportStation>
    {
        public PlaneController PlaneController { get; set; }

        public void Init(ExportStation exportStation)
        {
            
        }

        public void EpisodeReset()
        {
            
        }

        private void Awake()
        {
            //_textMesh = FloatingTextObject.GetComponent<TextMeshPro>();
        }

        public Item GetItem(string itemType)
        {
            return null;
        }

        public ExchangeMessage CheckGivable(IExchangeable receiver, Item item)
        {
            return ExchangeMessage.Ungivable;
        }

        public ExchangeMessage CheckReceivable(IExchangeable giver, Item item)
        {
            if (item.itemState.type==SpecialItemStateType.Product)
            {
                return ExchangeMessage.Ok;
            }
            return ExchangeMessage.WrongType;
        }

        public bool Store(Item item)
        {
            PlaneController.DeliverProduct(item);
            Destroy(item.gameObject);
            return true;
        }

        public bool Remove(Item item)
        {
            return false;
        }
    }
}
