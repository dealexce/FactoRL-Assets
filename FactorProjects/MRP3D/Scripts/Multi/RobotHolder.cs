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

        public override Item GetItem()
        {
            return item;
        }

        // Start is called before the first frame update
        void Start()
        {
        
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
            Debug.Log("item removed");
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
            if (other.CompareTag("input_plate"))
            {
                ItemHolder receiver = (ItemHolder) other.GetComponentInParent<WorkStationController>();
                Give(receiver);
            }
            if (other.CompareTag("output_plate"))
            {
                other.GetComponentInParent<WorkStationController>().Give(this);
            }
            if (other.CompareTag("raw_stack"))
            {
                other.GetComponent<RawStackController>().Give(this);
            }
            if (other.CompareTag("export_plate"))
            {
                ItemHolder receiver = (ItemHolder) other.GetComponentInParent<ExportPlateController>();
                Give(receiver);
            }
        }

        public void ResetHolder()
        {
            this.item = null;
        }


    }
}

