using System;
using TMPro;
using UnityEngine;

namespace FactorProjects.MRP3D.Scenes.CMSv3.Scripts
{
    public class ExportController : MonoBehaviour, IExchangable, ILinkedToPlane, IResetable
    {
        public PlaneController planeController { get; set; }
        public GameObject FloatingTextObject;
        private TextMeshPro _textMesh;

        public void EpisodeReset()
        {
            
        }

        private void Awake()
        {
            _textMesh = FloatingTextObject.GetComponent<TextMeshPro>();
        }

        public Item GetItem(string itemType)
        {
            return null;
        }

        public ExchangeMessage CheckGivable(IExchangable receiver, Item item)
        {
            return ExchangeMessage.Ungivable;
        }

        public ExchangeMessage CheckReceivable(IExchangable giver, Item item)
        {
            if (item.itemState.type==SpecialItemStateType.Product)
            {
                return ExchangeMessage.Ok;
            }
            return ExchangeMessage.WrongType;
        }

        public bool Store(Item item)
        {
            planeController.DeliverProduct(item);
            Destroy(item.gameObject);
            return true;
        }

        public bool Remove(Item item)
        {
            return false;
        }
    }
}
