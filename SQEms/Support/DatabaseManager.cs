using System;
using System.Collections.Generic;
using System.IO;
using System.Configuration;
using Definitions;

namespace Support
{
	/// <summary>
	/// A class which handles database initialization, and instantiation.
	/// Contains the prototypes for every database.
	/// </summary>
	public class DatabaseManager
	{
		private static Dictionary<string, Tuple<string, DatabaseTablePrototype>> TablePrototypes =
			new Dictionary<string, Tuple<string, DatabaseTablePrototype>>();

		/// <summary>
		/// Sets up the database table prototypes.
		/// </summary>
		static DatabaseManager()
		{
			AddTable(new TestTable(), "./test_table.dat");

			AddTable (new PeopleTable (), "./people.dat");
			AddTable (new AppointmentTable (), "./appointments.dat");
			AddTable (new HouseholdTable (), "./households.dat");
		}

		/// <summary>
		/// Helper to add a table prototype
		/// </summary>
		/// <param name="proto">The prototype</param>
		/// <param name="file">The physical file to load from and save to</param>
		private static void AddTable(DatabaseTablePrototype proto, string file)
		{

			TablePrototypes [proto.Name] = new Tuple<string, DatabaseTablePrototype> (file, proto);
		}

		private Dictionary<string, DatabaseTable> tables = new Dictionary<string, DatabaseTable> ();

		public DatabaseManager()
		{
			foreach (string name in TablePrototypes.Keys)
			{
				tables [name] = new DatabaseTable(TablePrototypes[name].Item1, TablePrototypes[name].Item2);
			}
		}

		/// <summary>
		/// Saves all tables.
		/// </summary>
		public void SaveAll()
		{
			foreach (string name in tables.Keys)
			{
				tables [name].Save ();
			}
		}

		/// <summary>
		/// Loads all tables.
		/// </summary>
		public void LoadAll()
		{
			foreach (string name in tables.Keys)
			{
				tables [name].Load ();
			}
		}

		/// <summary>
		/// Gets a table by name.
		/// </summary>
		/// <param name="name">The table's name</param>
		public DatabaseTable this[string name]
		{
			get{ return tables [name];}
		}

	}

	/// <summary>
	/// A class representing a database table
	/// </summary>
	public class DatabaseTable
	{
		/// <summary>
		/// The table's prototype
		/// </summary>
		private DatabaseTablePrototype prototype;

		/// <summary>
		/// The table's physical file.
		/// </summary>
		public string Location { get; private set; }

		/// <summary>
		/// The table's data.
		/// </summary>
		private SortedDictionary<object, object[]> Data = new SortedDictionary<object, object[]>();

		public DatabaseTable(string file, DatabaseTablePrototype prototype)
		{
			this.Location = file;
			this.prototype = prototype;
		}

		/// <summary>
		/// Loads this table from the file.
		/// </summary>
		public void Load()
		{
			using (BinaryReader input = new BinaryReader (new FileStream(Location, FileMode.OpenOrCreate))) {
				while (input.PeekChar() != -1) {
					object[] row = prototype.LoadRowImpl (input);

					Data [row [prototype.PrimaryKeyIndex]] = row;
				}
			}
		}

		/// <summary>
		/// Saves this table to the file.
		/// Also creates a backup with path '<path>~'.
		/// </summary>
		public void Save()
		{
			// only save if the prototype isn't read only
			if (!prototype.ReadOnly)
			{
				// save a backup
				File.Move (Location, Location + "~");

                using (BinaryWriter output = new BinaryWriter(new FileStream(Location, FileMode.Create)))
                {
                    foreach (object[] row in Data.Values)
                    {
                        prototype.SaveRowImpl(output, row);
                    }
                }
			}
		}

		/// <summary>
		/// Inserts a new row into this table. Primary key must be given already.
		/// Types must match. Doesn't check if primary key is already taken.
		/// </summary>
		/// <param name="columns">The columns to insert</param>
		public void Insert(params object[] columns)
		{
			if (columns.Length != prototype.Columns.Length)
			{
				throw new ArgumentException ("Invalid column length");
			}

			for (int i = 0; i < columns.Length; i++)
			{
				if (columns [i].GetType () != prototype.ColumnTypes [i])
				{
					throw new ArgumentException(string.Format("Object at index {0} has invalid type {1}, must be {2}",
					                                          i, columns[i].GetType(), prototype.ColumnTypes[i]));
				}
			}

			Data [columns [prototype.PrimaryKeyIndex]] = columns;
		}

		/// <summary>
		/// Gets or sets a column from a specific row.
		/// </summary>
		/// <param name="primary_key">The row's primary key</param>
		/// <param name="column">The column</param>
		public object this[object primary_key, string column]
		{
			get{
				if (Data.ContainsKey (primary_key))
				{
					if (prototype.ColumnsReverse.ContainsKey(column))
					{
						return Data [primary_key] [prototype.ColumnsReverse[column]];
					}
					else
					{
						throw new ArgumentException ("Invalid column");
					}
				}
				else
				{
					throw new ArgumentException ("Invalid Primary Key");
				}
			}

			set{
				if (!prototype.ColumnsReverse.ContainsKey (column))
				{
					throw new ArgumentException ("Invalid column");
				}
				
				int index = prototype.ColumnsReverse[column];
				object v = value;

				// check that the types are correct
				if (v.GetType () != prototype.ColumnTypes [index])
				{
					throw new ArgumentException (string.Format ("Invalid type {0} must be {1}",
					                                            v.GetType (), prototype.ColumnTypes [index]));
				}

				if (Data.ContainsKey (primary_key))
				{
					Data [primary_key] [index] = v;
				}
				else
				{
					throw new ArgumentException ("Invalid Primary Key");
				}
			}
		}

	}

	/// <summary>
	/// A class which represents the meta-data for a table.
	/// A work-around for no virtual static methods.
	/// </summary>
	public abstract class DatabaseTablePrototype
	{
		public string Name { get; protected set; }

		public string[] Columns { get; protected set; }

		public readonly Dictionary<string, int> ColumnsReverse = new Dictionary<string, int>();

		public Type[] ColumnTypes { get; protected set; }

		public int PrimaryKeyIndex { get; protected set; }

		public bool ReadOnly { get; protected set; }

		private static Dictionary<Type, Func<BinaryReader, object>> Readers =
			new Dictionary<Type, Func<BinaryReader, object>>();

		private static Dictionary<Type, Action<BinaryWriter, object>> Writers =
			new Dictionary<Type, Action<BinaryWriter, object>>();

		protected Func<BinaryReader, object>[] ColumnReaders;

		protected Action<BinaryWriter, object>[] ColumnWriters;

		static DatabaseTablePrototype()
		{
			Readers [typeof(string)] = r => r.ReadString ();
			Writers [typeof(string)] = (w, o) => w.Write ((string)o);
			
			Readers [typeof(Int32)] = r => r.ReadInt32 ();
			Writers [typeof(Int32)] = (w, o) => w.Write ((Int32)o);
			
			Readers [typeof(char)] = r => r.ReadChar ();
			Writers [typeof(char)] = (w, o) => w.Write ((char)o);
		}

		public DatabaseTablePrototype(int size)
		{
			ColumnReaders = new Func<BinaryReader, object>[size];
			ColumnWriters = new Action<BinaryWriter, object>[size];
		}

		/// <summary>
		/// Must be called after initialization. Sets up any cache members.
		/// </summary>
		protected void PostInit()
		{
			for (int i = 0; i < Columns.Length; i++)
			{
				ColumnsReverse [Columns [i]] = i;

				if (ColumnReaders [i] == null)
				{
					if (Readers.ContainsKey (ColumnTypes [i]))
					{
						ColumnReaders [i] = Readers [ColumnTypes [i]];
					}
					else
					{
						System.Diagnostics.Debug.WriteLine ("Warning: Column {0} has no Reader and requires one", i);
					}
				}
				
				if (ColumnWriters [i] == null)
				{
					if (Writers.ContainsKey (ColumnTypes [i]))
					{
						ColumnWriters [i] = Writers [ColumnTypes [i]];
					}
					else
					{
						System.Diagnostics.Debug.WriteLine ("Warning: Column {0} has no Writer and requires one", i);
					}
				}
			}

		}

		/// <summary>
		/// Loads a row.
		/// </summary>
		/// <returns>The row</returns>
		/// <param name="reader">The reader to read from</param>
		public virtual object[] LoadRowImpl (BinaryReader reader)
		{
			object[] o = new object[ColumnTypes.Length];

			for (int i = 0; i < o.Length; i++)
			{
				o [i] = ColumnReaders [i].Invoke (reader);
			}

			return o;
		}

		/// <summary>
		/// Saves a row.
		/// </summary>
		/// <param name="writer">The writer to write to</param>
		/// <param name="row">The row to save</param>
		public virtual void SaveRowImpl (BinaryWriter writer, object[] row)
		{
			for (int i = 0; i < ColumnTypes.Length; i++)
			{
				ColumnWriters [i].Invoke (writer, row [i]);
			}
		}
	}

	/// <summary>
	/// An example table prototype.
	/// </summary>
	class TestTable: DatabaseTablePrototype
	{

		#region implemented abstract members of DatabaseTablePrototype

		public TestTable ():
			base(2)
		{
			Name = "TestTable";

			Columns = new string[]{"pk", "one"};

			ColumnTypes = new Type[] { typeof(Int32), typeof(string) };

			PrimaryKeyIndex = 0;

			base.PostInit ();
		}

		#endregion

	}
	
	/// <summary>
	/// The prototype for the patient table.
	/// </summary>
	/// <remarks>
	/// Fields:
	/// Int32 - PatientID (PK)
	/// string- HCN
	/// string - lastName
	/// string - firstName
	/// char - mInitial
	/// string- dateBirth
	/// SexTypes - sex
	/// Int32 - HouseID (FK for household)
	/// </remarks>
	class PeopleTable: DatabaseTablePrototype
	{

		#region implemented abstract members of DatabaseTablePrototype

		public PeopleTable ():
			base(8)
		{
			Name = "Patients";

			Columns = new string[]{
				"PatientID",
				"HCN",
				"lastName",
				"firstName",
				"mInitial",
				"dateBirth",
				"sex",
				"HouseID"
			};

			ColumnTypes = new Type[] {
				typeof(Int32),
				typeof(string),
				typeof(string),
				typeof(string),
				typeof(char),
				typeof(string),
				typeof(SexTypes),
				typeof(Int32)
			};

			ColumnReaders [6] = (r) => (SexTypes)r.ReadInt32();

			ColumnWriters [6] = (r, o) => r.Write(Convert.ToInt32(o));

			PrimaryKeyIndex = 0;

			base.PostInit ();
		}

		#endregion

	}

	/// <summary>
	/// The prototype for the appointment table.
	/// </summary>
	class AppointmentTable: DatabaseTablePrototype
	{
		#region implemented abstract members of DatabaseTablePrototype
		
		public AppointmentTable ():
			base(7)
		{
			Name = "Appointments";

			Columns = new string[]{
				"AppointmentID",
				"Month",
				"Week",
				"Day",
				"TimeSlot",
				"PatientID",
				"CaregiverID"
			};

			ColumnTypes = new Type[] {
				typeof(Int32),
				typeof(Int32),
				typeof(Int32),
				typeof(Int32),
				typeof(Int32),
				typeof(Int32),
				typeof(Int32)
			};

			PrimaryKeyIndex = 0;

			base.PostInit ();
		}

		#endregion


	}

	/// <summary>
	/// The prototype for the household table.
	/// </summary>
	class HouseholdTable: DatabaseTablePrototype
	{
		#region implemented abstract members of DatabaseTablePrototype

		public HouseholdTable ():
			base(7)
		{
			Name = "Household";

			Columns = new string[]{
				"HouseID",
				"addressLine1",
				"addressLine2",
				"city",
				"province",
				"numPhone",
				"HeadOfHouseHCN"
			};

			ColumnTypes = new Type[] {
				typeof(Int32),
				typeof(string),
				typeof(string),
				typeof(string),
				typeof(string),
				typeof(string),
				typeof(string)
			};

			PrimaryKeyIndex = 0;

			base.PostInit ();
		}

		#endregion


	}
	
	class BillingMasterTable: DatabaseTablePrototype
	{
		#region implemented abstract members of DatabaseTablePrototype

		public BillingMasterTable():
			base(4)
		{
			Name = "BillingMaster";

			Columns = new string[]{"BillingCode", "EffectiveDate", "DollarAmount", "ResponseState"};

			ColumnTypes = new Type[] { typeof(string), typeof(string), typeof(string),
				typeof(Int32)};

			PrimaryKeyIndex = 0;

			ReadOnly = true;

			base.PostInit ();
		}

		public override object[] LoadRowImpl (BinaryReader reader)
		{
			// binaryreaders can't read a line, so a streamreader should be used
			StreamReader input = new StreamReader (reader.BaseStream);

			//input.readline should be called once, then the string should be parsed

			return null;
		}

		public override void SaveRowImpl (BinaryWriter writer, object[] row)
		{
			// table is read-only, do nothing
		}

		#endregion

	}

}




