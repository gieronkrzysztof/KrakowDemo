using System;
using System.Collections.Generic;
using System.Text;

namespace KrakowDemo.DTOs
{
    public class OrderDTO
    {
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public int SizeY { get; set; }
        public int SizeX { get; set; }
        public string Filename { get; set; }
    }
}
