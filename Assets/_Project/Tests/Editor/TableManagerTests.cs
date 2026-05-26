using NUnit.Framework;
using CafeSim.Core.Tables;

namespace CafeSim.Tests.Core.Tables
{
    /// <summary>
    /// Tests de gestión de mesas y sillas. Verifica los casos críticos:
    /// asignación correcta, lleno, liberación, capacidad agregada.
    /// </summary>
    [TestFixture]
    public class TableManagerTests
    {
        [Test]
        public void Constructor_ComputesTotalCapacity()
        {
            var tm = new TableManager(tableCount: 3, seatsPerTable: 4);
            Assert.AreEqual(3, tm.TableCount);
            Assert.AreEqual(12, tm.TotalCapacity);
            Assert.AreEqual(12, tm.FreeSeats);
            Assert.IsFalse(tm.IsFull);
        }

        [Test]
        public void TryAssignSeat_FillsTablesInOrder()
        {
            var tm = new TableManager(tableCount: 2, seatsPerTable: 2);

            tm.TryAssignSeat(1, out int t1, out _);
            tm.TryAssignSeat(2, out int t2, out _);
            tm.TryAssignSeat(3, out int t3, out _);

            // Las primeras dos sillas son de la mesa 1, la tercera ya es de la mesa 2.
            Assert.AreEqual(1, t1);
            Assert.AreEqual(1, t2);
            Assert.AreEqual(2, t3);
        }

        [Test]
        public void TryAssignSeat_ReturnsFalseWhenFull()
        {
            var tm = new TableManager(tableCount: 1, seatsPerTable: 1);
            Assert.IsTrue(tm.TryAssignSeat(1, out _, out _));
            Assert.IsFalse(tm.TryAssignSeat(2, out int tableId, out int seatIndex));
            Assert.AreEqual(-1, tableId);
            Assert.AreEqual(-1, seatIndex);
            Assert.IsTrue(tm.IsFull);
        }

        [Test]
        public void ReleaseSeat_FreesCapacityForReassignment()
        {
            var tm = new TableManager(tableCount: 1, seatsPerTable: 2);
            tm.TryAssignSeat(1, out int table, out _);
            tm.TryAssignSeat(2, out _, out _);

            Assert.IsTrue(tm.IsFull);
            tm.ReleaseSeat(1, table);
            Assert.IsFalse(tm.IsFull);

            Assert.IsTrue(tm.TryAssignSeat(3, out _, out _));
        }

        [Test]
        public void Clear_ReleasesAllSeats()
        {
            var tm = new TableManager(tableCount: 2, seatsPerTable: 2);
            tm.TryAssignSeat(1, out _, out _);
            tm.TryAssignSeat(2, out _, out _);
            tm.TryAssignSeat(3, out _, out _);
            tm.Clear();
            Assert.AreEqual(tm.TotalCapacity, tm.FreeSeats);
        }

        [Test]
        public void TableManager_ZeroTables_NeverAssigns()
        {
            var tm = new TableManager(tableCount: 0, seatsPerTable: 4);
            Assert.AreEqual(0, tm.TotalCapacity);
            Assert.IsTrue(tm.IsFull);
            Assert.IsFalse(tm.TryAssignSeat(1, out _, out _));
        }
    }
}
