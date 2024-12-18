using ITValet.HelpingClasses;
using ITValet.Models;

namespace ITValet.Utils.Helpers
{
    public static class MappingHelper
    {
        public static List<OrderDtoList> MapOrdersToDtos(IEnumerable<Order> orders)
        {
            var orderDtos = new List<OrderDtoList>();

            foreach (var order in orders)
            {
                var orderDto = new OrderDtoList
                {
                    Id = order.Id.ToString(),
                    EncId = StringCipher.EncryptId(order.Id),
                    OrderTitle = order.OrderTitle,
                    StartDateTime = order.StartDateTime?.ToString() ?? "",
                    EndDateTime = order.EndDateTime?.ToString() ?? "",
                    OrderPrice = order.OrderPrice.ToString(),
                    IsDelivered = order.IsDelivered.ToString()
                };

                if (order.OrderReason != null && order.OrderReason.Any())
                {
                    var reason = order.OrderReason.FirstOrDefault();
                    if (reason != null)
                    {
                        orderDto.OrderReasonId = reason.Id.ToString();
                        orderDto.OrderReasonExplanation = reason.ReasonExplanation;
                        orderDto.OrderReasonType = reason.ReasonType.ToString();
                        orderDto.OrderReasonIsActive = reason.IsActive.ToString();
                    }
                }

                orderDtos.Add(orderDto);
            }

            return orderDtos;
        }

    }
}
