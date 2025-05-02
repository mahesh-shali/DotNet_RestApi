import pyodbc

connection_string = (
    "Driver={ODBC Driver 18 for SQL Server};"
    "Server=tcp:web-dot-net.database.windows.net,1433;"
    "Database=web-dot-net;"
    "Uid=vmadmin;"
    "Pwd=Admin@123;"
    "Encrypt=yes;"
    "TrustServerCertificate=no;"
    "Connection Timeout=30;"
)

try:
    conn = pyodbc.connect(connection_string)
    print("Connection successful!")
    conn.close()
except Exception as e:
    print(f"Error: {e}")
