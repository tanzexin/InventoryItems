using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.DTO
{
	public class OrderDto
	{
		public long addressId; // 收获人地址id
		public byte paymentType;// 付款类型
		public List<CartDto> carts=new List<CartDto> ();// 订单详情
	}
}
