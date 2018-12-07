using System;
using System.Collections.Generic;
using System.IO;
using System.Configuration;
using Definitions;
using System.Linq;
using System.Collections;

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
            // call the custom reader if there is one
            if(prototype.CustomReader != null)
            {
                prototype.CustomReader(this, File.ReadAllText(Location));
                return;
            }

            using (BinaryReader input = new BinaryReader(new FileStream(Location, FileMode.OpenOrCreate)))
            {
                while (input.PeekChar() != -1)
                {
                    object[] row = prototype.LoadRowImpl(input);

                    Data[row[prototype.PrimaryKeyIndex]] = row;
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
                if (File.Exists(Location))
                {
                    // save a backup, if possible
                    File.Move(Location, Location + "~");
                }

                // call the custom writer if there is one
                if(prototype.CustomWriter != null)
                {
                    File.WriteAllText(Location, prototype.CustomWriter(this));
                    return;
                }

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
        /// Gets the primary keys for rows which match a certain condition.
        /// </summary>
        /// <typeparam name="T">The type to check against.</typeparam>
        /// <param name="column">The column to check.</param>
        /// <param name="predicate">The condition.</param>
        /// <returns>The primary keys,.</returns>
        public IEnumerable<object> Where<T>(string column, Func<T, bool> predicate)
        {
            int column_index = NameToIndex(column);

            if (column_index == -1)
            {
                throw new ArgumentException("Column '" + column + "' doesn't exist in the given table");
            }

            if (typeof(T) != prototype.ColumnTypes[column_index])
            {
                throw new ArgumentException("Column '" + column + "' is type " + prototype.ColumnTypes[column_index] + " not " + typeof(T));
            }

            foreach (Tuple<object, object> row in this[column])
            {
                if (predicate((T)row.Item1))
                {
                    yield return row.Item2;
                }
            }

            yield break;
        }

        /// <summary>
        /// Gets all primary keys for rows where the specified column equals a specific value.
        /// </summary>
        /// <remarks>
        /// 
        /// I recommend using this method with System.Linq, because it includes many helper methods.
        /// 
        /// Example (using Linq):
        /// 
        /// <code>
        /// 
        /// object pk = People.WhereEquals<string>("firstName", "[insert first name]").First();
        /// 
        /// object lastName = People[pk, "lastName"];
        /// 
        /// do something with lastName, etc.
        /// 
        /// </code>
        /// 
        /// Example (not using Linq):
        /// 
        /// <code>
        /// 
        /// foreach(object pk in People.WhereEquals<string>("firstName", "[insert first name]"))
        /// {
        ///     // this person has a first name which matches the given input, do something with it.
        /// }
        /// 
        /// </code>
        /// 
        /// </remarks>
        /// <typeparam name="T">The column type.</typeparam>
        /// <param name="column">The column to compare against.</param>
        /// <param name="equals">The value to compare against.</param>
        /// <returns>The primary keys.</returns>
        public IEnumerable<object> WhereEquals<T>(string column, T equals)
        {
            return Where<T>(column, t => object.Equals(t, equals));
        }

		/// <summary>
		/// Gets all primary keys which rows match the required columns and objects.
		/// </summary>
		/// <param name="columns">The semi-colon separated columns.</param>
		/// <param name="objs">The objects to check.</param>
		/// <returns>All primary keys.</returns>
		public IEnumerable<object> WhereEquals(string columns, params object[] objs)
		{
			string[] cols = columns.Split (';');
			int[] col_indices = new int[cols.Length];

			// check columns lengths
			if (cols.Length != objs.Length)
			{
				throw new ArgumentException("Length mismatch between columns and objects");
			}

			// cache the column indices, and check if the columns are valid
			for (int i = 0; i < cols.Length; i++)
			{
				col_indices [i] = NameToIndex(cols [i]);

				if (col_indices[i] == -1)
				{
					throw new ArgumentException ("Invalid column '" + cols [i] + "'");
				}
			}

			// for each row, return the row if it matches
			foreach (object pk in Data.Keys)
			{
				bool matches = true;

				object[] row = Data [pk];

				// check each column if they're equal
				for (int i = 0; i < cols.Length; i++)
				{
					if (!Equals (objs [i], row [col_indices [i]]))
					{
						matches = false;
						break;
					}
				}

				if (matches)
				{
					// signals to the runtime that this method should stop running (temporarily)
					// and return 'pk' to the for loop.
					yield return pk;
				}
			}
		}

        private static int WhereEqualsInt = 0;
		
		/// <summary>
		/// Gets all rows which equal a value.
		/// 
		/// Not thread safe.
		/// </summary>
		/// <param name="columns">The columns to compare against.</param>
		/// <param name="equals">The values to compare against.</param>
		/// <returns>The primary keys.</returns>
        public IEnumerable<object> WhereEquals2(string[] columns, params int[] equals)
        {
			// you guys really should've commented this, it took me a couple
			// minutes to figure out what this was doing

            DatabaseManager databaseManager = new DatabaseManager();
            DatabaseTable tmpTable = databaseManager["Appointments"];

            IEnumerable<object> retObject = null;
            foreach (object key in this.WhereEquals<int>(columns[WhereEqualsInt], equals[WhereEqualsInt]))
            {
                tmpTable.Insert(this[key, "AppointmentID"],
                                this[key, "Month"],
                                this[key, "Week"],
                                this[key, "Day"],
                                this[key, "TimeSlot"],
                                this[key, "PatientID"],
                                this[key, "CaregiverID"]);
            }

            if (WhereEqualsInt == columns.Length - 1)
            {
                retObject = this.WhereEquals<int>(columns[WhereEqualsInt], equals[WhereEqualsInt]);
            }
            else
            {
                WhereEqualsInt++;
                return tmpTable.WhereEquals2(columns, equals);
            }

            WhereEqualsInt = 0;

            return retObject;
        }

        /// <summary>
        /// Gets the maximum for a specific column, if the column is a type int.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <returns>The maximum value.</returns>
        public int GetMaximum(string column)
        {
            int column_index = NameToIndex(column);

            if (column_index == -1)
            {
                throw new ArgumentException("Column '" + column + "' doesn't exist in the given table");
            }

            if (prototype.ColumnTypes[column_index] != typeof(int))
            {
                throw new ArgumentException("Column '" + column + "' is type " + prototype.ColumnTypes[column_index] + " not int");
            }

            int max = 0;

            foreach(Tuple<object, object> row in this[column])
            {
                int val = (int)row.Item1;

                if(val > max)
                {
                    max = val;
                }
            }

            return max;
        }

        /// <summary>
        /// Gets all rows in the format {specified column, primary key}
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public IEnumerable<Tuple<object, object>> this[string column]
        {
            get {

                int column_index = NameToIndex(column);

                if(column_index == -1)
                {
                    throw new ArgumentException("Column '" + column + "' doesn't exist in the given table");
                }

                foreach (object pk in Data.Keys)
                {
                    yield return new Tuple<object, object>(Data[pk][column_index], pk);
                }

                yield break;
            }
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

        /// <summary>
        /// Gets the index for a column by name.
        /// </summary>
        /// <param name="name">The column name.</param>
        /// <returns>The column index, or -1 if it doesn't exist.</returns>
        private int NameToIndex(string name)
        {
            return prototype.ColumnsReverse.ContainsKey(name) ? 
                    prototype.ColumnsReverse[name] : -1;
        }

	}

	/// <summary>
	/// A class which represents the meta-data for a table.
	/// A work-around for no virtual static methods.
	/// </summary>
	public abstract class DatabaseTablePrototype
	{
        /// <summary>
        /// This table's name.
        /// </summary>
		public string Name { get; protected set; }

        /// <summary>
        /// This table's column names.
        /// </summary>
		public string[] Columns { get; protected set; }

        /// <summary>
        /// A lookup to help with finding the index of a column.
        /// </summary>
		public readonly Dictionary<string, int> ColumnsReverse = new Dictionary<string, int>();

		public Type[] ColumnTypes { get; protected set; }

		public int PrimaryKeyIndex { get; protected set; }

		public bool ReadOnly { get; protected set; }

		protected Func<BinaryReader, object>[] ColumnReaders;

		protected Action<BinaryWriter, object>[] ColumnWriters;

        /// <summary>
        /// A custom reader for this database table.
        /// </summary>
        /// <remarks>
        /// When specified, <see cref="LoadRowImpl(BinaryReader)"/> is not called.
        /// </remarks>
        public Action<DatabaseTable, string> CustomReader { get; protected set; }

        /// <summary>
        /// A custom writer for this database table.
        /// </summary>
        /// <remarks>
        /// When specified, <see cref="SaveRowImpl(BinaryReader)"/> is not called.
        /// </remarks>
        public Func<DatabaseTable, string> CustomWriter { get; protected set; }

        /// <summary>
        /// All pre-defined readers.
        /// </summary>
        private static Dictionary<Type, Func<BinaryReader, object>> Readers =
            new Dictionary<Type, Func<BinaryReader, object>>();

        /// <summary>
        /// All pre-defined writers.
        /// </summary>
        private static Dictionary<Type, Action<BinaryWriter, object>> Writers =
            new Dictionary<Type, Action<BinaryWriter, object>>();


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

}




