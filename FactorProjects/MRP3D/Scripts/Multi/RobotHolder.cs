using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

namespace Multi
{
    public class RobotHolder : ItemHolder
    {
        private Item item;
        private RobotMoveAgent _robotMoveAgent;

        public override Item GetItem()
        {
            return item;
        }

        // Start is called before the first frame update
        void Start()
        {
            _robotMoveAgent = GetComponent<RobotMoveAgent>();
        }

        // Update is called once per frame
        void Update()
        {
            if (item != null)
            {
                item.gameObject.transform.position = transform.position + Vector3.up * .5f;
            }
        }
        
        protected override bool Store(Item item)
        {
            if (this.item == null)
            {
                this.item = item;
                return true;
            }
            return false;
        }

        protected override bool Remove(Item item)
        {
            this.item = null;
            return true;
        }

        public override ExchangeMessage CheckOfferable(ItemHolder requesters,Item item)
        {
            if (this.item != item)
            {
                return ExchangeMessage.ItemNotFound;
            }
            return ExchangeMessage.OK;
        }

        public override ExchangeMessage CheckReceivable(ItemHolder giver, Item item)
        {
            if (this.item == null)
            {
                return ExchangeMessage.OK;
            }
            return ExchangeMessage.Overload;
        }

        private void OnTriggerEnter(Collider other)
        {
            ExchangeMessage exchangeMessage = ExchangeMessage.Fail;
            if (other.tag.Contains("input"))
            {
                ItemHolder receiver = (ItemHolder) other.GetComponentInParent<WorkStationController>();
                exchangeMessage=Give(receiver);
            }
            if (other.tag.Contains("output"))
            {
                exchangeMessage=other.GetComponentInParent<WorkStationController>().Give(this);
            }
            if (other.CompareTag("raw_stack"))
            {
                exchangeMessage=other.GetComponent<RawStackController>().Give(this);
            }
            if (other.CompareTag("export_plate"))
            {
                ItemHolder receiver = (ItemHolder) other.GetComponentInParent<ExportPlateController>();
                exchangeMessage=Give(receiver);
            }
            _robotMoveAgent.OnRobotHolderTriggerEnter(other, exchangeMessage);
            
        }

        public void ResetHolder()
        {
            if (item != null)
            {
                Destroy(item.gameObject);
            }
            this.item = null;
        }


    }
}

