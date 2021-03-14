using Model.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StockInterface
{
     public interface IStockService
    {
         void IncreaseStock();
         void DecreaseStock(List<CartDto> cartDtos, long orderId);
         void ResumeStock(List<CartDto> cartDtos, long orderId);
    }
}
