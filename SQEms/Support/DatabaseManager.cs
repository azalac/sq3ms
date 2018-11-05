using System;
using System.Collections.Generic;
using System.IO;
using System.Configuration;

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
			AddTable(new TestTable(), "./db_file.dat");
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
			// save a backup
			File.Move (Location, Location + "~");

			using (BinaryWriter output = new BinaryWriter(new FileStream(Location, FileMode.Create)))
			{
				foreach (object[] row in Data.Values)
				{
					prototype.SaveRowImpl (output, row);
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

		/// <summary>
		/// Must be called after initialization. Sets up any cache members.
		/// </summary>
		protected void PostInit()
		{
			for (int i = 0; i < Columns.Length; i++)
			{
				ColumnsReverse [Columns [i]] = i;
			}
		}

		/// <summary>
		/// Loads a row.
		/// </summary>
		/// <returns>The row</returns>
		/// <param name="reader">The reader to read from</param>
		public abstract object[] LoadRowImpl (BinaryReader reader);

		/// <summary>
		/// Saves a row.
		/// </summary>
		/// <param name="writer">The writer to write to</param>
		/// <param name="row">The row to save</param>
		public abstract void SaveRowImpl (BinaryWriter writer, object[] row);
	}

	/// <summary>
	/// An example table prototype.
	/// </summary>
	public class TestTable: DatabaseTablePrototype
	{

		#region implemented abstract members of DatabaseTablePrototype

		public TestTable ()
		{
			Name = "TestTable";

			Columns = new string[]{"pk", "one"};

			ColumnTypes = new Type[] { typeof(Int32), typeof(string) };

			PrimaryKeyIndex = 0;

			base.PostInit ();

		}

		public override object[] LoadRowImpl (BinaryReader reader)
		{
			return new object[] { reader.ReadInt32(), reader.ReadString() };
		}

		public override void SaveRowImpl (BinaryWriter writer, object[] row)
		{
			writer.Write ((Int32)row [0]);
			writer.Write ((string)row [1]);
		}

		#endregion

	}

}




