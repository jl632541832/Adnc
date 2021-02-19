﻿using Adnc.Core.Shared.Events;

namespace Adnc.Orders.Domain.Events.Etos
{
    public class OrderInventoryFreezedEventEto : BaseEto
    {
        public long OrderId { get; set; }

        public bool IsSuccess { get; set; }
    }
}