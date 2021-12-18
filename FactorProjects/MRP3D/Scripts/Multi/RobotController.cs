using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

namespace Multi
{
    public class RobotController : ItemHolder
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
            item.gameObject.transform.position = transform.position + Vector3.up * .5f;

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

        protected override void Remove(Item item)
        {
            this.item = null;
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
                other.GetComponentInParent<WorkStationController>().Give(this);
            }
        }


    }
}

