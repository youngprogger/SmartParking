using System;

namespace ParkingApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var pm = new ParkingManager();
            pm.GetAllData();
            pm.EnterParking("2123");
            Console.WriteLine(pm.GetRemainingCost(0));
        }
    }
}
