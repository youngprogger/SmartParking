using SmartParkingApp;
using System;
using System.Collections.Generic;
using System.IO;

namespace ParkingApp
{
    class ParkingManager
    {
        private List<Tariff> priceList = new List<Tariff>();
        private int freeParkingPlaces;
        private int freeTime;
        private List<User> users = new List<User>();
        private List<ParkingSession> activeSessions = new List<ParkingSession>();
        private List<ParkingSession> passiveSessions = new List<ParkingSession>();
        public ParkingSession EnterParking(string carPlateNumber)
        {
            if (freeParkingPlaces > activeSessions.Count)
            {
                bool nummerCheck = true;
                foreach (var s in activeSessions)
                {
                    if (s.CarPlateNumber == carPlateNumber)
                        nummerCheck = false;
                }
                if (nummerCheck == true)
                {
                    var newElement = new ParkingSession();
                    newElement.EntryDt = DateTime.Now;
                    newElement.CarPlateNumber = carPlateNumber;
                    if (activeSessions.Count != 0)
                        newElement.TicketNumber = activeSessions[activeSessions.Count - 1].TicketNumber + 1;
                    else
                        newElement.TicketNumber = 0;
                    activeSessions.Add(newElement);
                    WriteSessionData();
                    return newElement;
                }
                else
                    return null;
            }
            else
                return null;
            /* Check that there is a free parking place (by comparing the parking capacity 
             * with the number of active parking sessions). If there are no free places, return null
             * 
             * Also check that there are no existing active sessions with the same car plate number,
             * if such session exists, also return null
             * 
             * Otherwise:
             * Create a new Parking session, fill the following properties:
             * EntryDt = current date time
             * CarPlateNumber = carPlateNumber (from parameter)
             * TicketNumber = unused parking ticket number = generate this programmatically
             * 
             * Add the newly created session to the list of active sessions
             * 
             * Advanced task:
             * Link the new parking session to an existing user by car plate number (if such user exists)            
             */
        }

        public bool TryLeaveParkingWithTicket(int ticketNumber, out ParkingSession session)
        {
            session = FindSession(ticketNumber);
            if (GetRemainingCost(ticketNumber) == 0)
            {
                session.ExitDt = DateTime.Now;
                activeSessions.Remove(session);
                passiveSessions.Add(session);
                WriteSessionData();
                return true;
            }
            else
            {
                session = null;
                return false;

            }
            /*
             * Check that the car leaves parking within the free leave period
             * from the PaymentDt (or if there was no payment made, from the EntryDt)
             * 1. If yes:
             *   1.1 Complete the parking session by setting the ExitDt property
             *   1.2 Move the session from the list of active sessions to the list of past sessions             * 
             *   1.3 return true and the completed parking session object in the out parameter
             * 
             * 2. Otherwise, return false, session = null
             */
        }

        public decimal GetRemainingCost(int ticketNumber)
        {
            var session = FindSession(ticketNumber);
            var time = FindParkingTime(session);
            var rate = FindTariff(time);
            return rate;
            /* Return the amount to be paid for the parking
             * If a payment had already been made but additional charge was then given
             * because of a late exit, this method should return the amount 
             * that is yet to be paid (not the total charge)
             */
        }






        public void PayForParking(int ticketNumber, decimal amount)
        {
            var session = FindSession(ticketNumber);
            if (session.TotalPayment == null)
                session.TotalPayment = amount;
            else
                session.TotalPayment += amount;
            /*
             * Save the payment details in the corresponding parking session
             * Set PaymentDt to current date and time
             * 
             * For simplicity we won't make any additional validation here and always
             * assume that the parking charge is paid in full
             */
        }


        public void GetAllData()
        {
            activeSessions = GetSessions("SessionFile.csv", "Active");
            GetSessions("SessionFile.csv", "Passive");
            GetUsers();
            GetTariffs("TariffFile.csv");
            GetCapacityandFreeLeaveTime("InformationFile.csv");

        }


        private void GetUsers()
        {
            var fileReader = File.ReadAllText("../../../UsersFile.csv");
            var listfileReader = fileReader.Split(new char[] { '\n' });
            List<User> tmpUsers = new List<User>();
            foreach (var s in listfileReader)
            {
                if (s != "")
                {
                    var tmpString = s.Split(new char[] { ',' });
                    {
                        var newUser = new User();
                        newUser.CarPlateNumber = tmpString[0];
                        newUser.Name = tmpString[1];
                        newUser.Phone = tmpString[3];
                        tmpUsers.Add(newUser);
                    }
                }
            }
            users = tmpUsers;
        }


        private decimal FindTariff(int? time)
        {
            if (time <= freeTime)
                return 0;
            else
            {
                if (priceList[priceList.Count - 1].Minutes <= time)
                    return priceList[priceList.Count - 1].Rate;
                else
                {
                    decimal rate = 0;
                    foreach (var t in priceList)
                    {
                        if (t.Minutes >= time)
                        {
                            rate = t.Rate;
                            break;
                        }
                    }
                    return rate;
                }
            }
        }


        private int? FindParkingTime(ParkingSession session)
        {
            int? time = 0;
            if (session.PaymentDt != null)
                time = Convert.ToInt32((DateTime.Now - session.PaymentDt)?.TotalMinutes);
            else
                time = Convert.ToInt32((DateTime.Now - session.EntryDt).TotalMinutes);
            return time;
        }


        private List<ParkingSession> GetSessions(string informationFile, string sessionType)
        {
            var fileReader = File.ReadAllText("../../../" + informationFile);
            var listfileReader = fileReader.Split(new char[] { '\n' });
            List<ParkingSession> tmpParkingSession = new List<ParkingSession>();
            foreach (var s in listfileReader)
            {
                var tmpString = s.Split(new char[] { ',' });
                if (tmpString[0] == sessionType)
                {
                    var newActiveSession = new ParkingSession();
                    newActiveSession.CarPlateNumber = tmpString[1];
                    newActiveSession.EntryDt = Convert.ToDateTime(tmpString[2]);
                    if (tmpString[3] != "")
                        newActiveSession.ExitDt = Convert.ToDateTime(tmpString[3]);
                    if (tmpString[4] != "")
                        newActiveSession.PaymentDt = Convert.ToDateTime(tmpString[4]);
                    newActiveSession.TicketNumber = Convert.ToInt32(tmpString[5]);
                    if (tmpString[6] != "")
                        newActiveSession.TotalPayment = Convert.ToDecimal(tmpString[6]);
                    tmpParkingSession.Add(newActiveSession);
                }
            }
            return tmpParkingSession;
        }


        private void GetCapacityandFreeLeaveTime(string informationFile)
        {
            var fileReader = File.ReadAllText("../../../" + informationFile);
            var tmpString = fileReader.Split(new char[] { ',' });
            freeParkingPlaces = Convert.ToInt32(tmpString[0]);
            freeTime = Convert.ToInt32(tmpString[1]);
        }


        private void GetTariffs(string InformationFile)
        {
            var fileReader = File.ReadAllText("../../../" + InformationFile);
            var listFileReader = fileReader.Split(new char[] { '\n' });
            foreach (var s in listFileReader)
            {
                var tmpString = s.Split(new char[] { ',' });
                var newTariff = new Tariff();
                newTariff.Minutes = Convert.ToInt32(tmpString[0]);
                newTariff.Rate = Convert.ToInt32(tmpString[1]);
                priceList.Add(newTariff);
            }
        }


        private void WriteSessionData()
        {
            var fileString = "";
            foreach (var s in activeSessions)
            {
                fileString += "Active," + s.CarPlateNumber + "," + s.EntryDt + "," + s.ExitDt + "," + s.PaymentDt + "," + s.TicketNumber + "," + s.TotalPayment + "\n";
            }
            foreach (var s in passiveSessions)
            {
                fileString += "Passive," + s.CarPlateNumber + "," + s.EntryDt + "," + s.ExitDt + "," + s.PaymentDt + "," + s.TicketNumber + "," + s.TotalPayment + "\n";
            }
            File.WriteAllText("../../../SessionFile.csv", fileString);
        }



        private ParkingSession FindSession(int ticketNumber)
        {
            var session = new ParkingSession();
            foreach (var k in activeSessions)
            {
                if (k.TicketNumber == ticketNumber)
                    session = k;
            }
            return session;
        }

        /* ADDITIONAL TASK 2 */
        public bool TryLeaveParkingByCarPlateNumber(string carPlateNumber, out ParkingSession session)
        {
            session = activeSessions.Find(e => e.CarPlateNumber == carPlateNumber);
            var checkUser = activeSessions.Exists(e => e.CarPlateNumber == carPlateNumber);
            if (checkUser == true)
            {
                session.ExitDt = DateTime.Now;
                session.EntryDt = session.EntryDt.AddMinutes(15);
                session.TotalPayment = GetRemainingCost(session.TicketNumber);
                session.PaymentDt = session.ExitDt;
                return true;
            }
            else if(GetRemainingCost(session.TicketNumber) == 0)
            {
                session.ExitDt = DateTime.Now;
                activeSessions.Remove(session);
                passiveSessions.Add(session);
                WriteSessionData();
                return true;
            }
            else
            {
                session = null;
                return false;
            }
            /* There are 3 scenarios for this method:
            
            1. The user has not made any payments but leaves the parking within the free leave period
            from EntryDt:
               1.1 Complete the parking session by setting the ExitDt property
               1.2 Move the session from the list of active sessions to the list of past sessions             * 
               1.3 return true and the completed parking session object in the out parameter
            
            2. The user has already paid for the parking session (session.PaymentDt != null):
            Check that the current time is within the free leave period from session.PaymentDt
               2.1. If yes, complete the session in the same way as in the previous scenario
               2.2. If no, return false, session = null

            3. The user has not paid for the parking session:            
            3a) If the session has a connected user (see advanced task from the EnterParking method):
            ExitDt = PaymentDt = current date time; 
            TotalPayment according to the tariff table:            
            
            IMPORTANT: before calculating the parking charge, subtract FreeLeavePeriod 
            from the total number of minutes passed since entry
            i.e. if the registered visitor enters the parking at 10:05
            and attempts to leave at 10:25, no charge should be made, otherwise it would be unfair
            to loyal customers, because an ordinary printed ticket could be inserted in the payment
            kiosk at 10:15 (no charge) and another 15 free minutes would be given (up to 10:30)

            return the completed session in the out parameter and true in the main return value

            3b) If there is no connected user, set session = null, return false (the visitor
            has to insert the parking ticket and pay at the kiosk)
            */
        }
    }
}
