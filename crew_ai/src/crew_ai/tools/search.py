from sqlalchemy import create_engine

# Define connection string
connection_string = "mssql+pyodbc://vmadmin:Admin@123@web-dot-net.database.windows.net/web-dot-net?driver=ODBC+Driver+17+for+SQL+Server"

# Create engine
engine = create_engine(connection_string)

# Connect to the database
with engine.connect() as connection:
    result = connection.execute("SELECT * FROM users")
    for row in result:
        print(row)
