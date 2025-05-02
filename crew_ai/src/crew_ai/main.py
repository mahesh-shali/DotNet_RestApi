#!/usr/bin/env python
import sys
import warnings
import urllib.parse
import yaml
from datetime import datetime
from sqlalchemy import create_engine, MetaData, Table, select
from crew_ai.crew import CrewAi

warnings.filterwarnings("ignore", category=SyntaxWarning, module="pysbd")

# Encode special characters in password
password = urllib.parse.quote_plus("Admin@123")

# Corrected connection string
DB_CONNECTION_STRING = f"mssql+pyodbc://vmadmin:{password}@web-dot-net.database.windows.net/web-dot-net?driver=ODBC+Driver+17+for+SQL+Server"

# Initialize database engine
engine = create_engine(DB_CONNECTION_STRING)

# Load metadata and check table
metadata = MetaData()
metadata.reflect(bind=engine)

print("📌 Tables found in the database:", metadata.tables.keys())  # Debugging

if "Users" not in metadata.tables:
    raise Exception("🚨 Table 'Users' does not exist. Check schema or database name.")

table = metadata.tables["Users"]

# Debug: Print table columns
print("📌 Columns in 'Users':", table.columns.keys())

# Load `category` from agents.yaml
def load_category_from_yaml():
    try:
        with open("agents.yaml", "r") as file:
            yaml_data = yaml.safe_load(file)
            return yaml_data.get("category", "AI LLMs")  # Default if missing
    except Exception as e:
        print(f"⚠️ Failed to load category from agents.yaml: {e}")
        return "AI LLMs"  # Fallback

def fetch_data_from_db():
    """
    Fetch data from the database.
    """
    try:
        with engine.connect() as conn:
            query = select(table)  # Removed `category` filter
            result = conn.execute(query)
            data = result.fetchall()

            if not data:
                print("⚠️ No records found in the database.")
            
            return data

    except Exception as e:
        raise Exception(f"Database query failed: {e}")

# def run():
#     """
#     Run the crew.
#     """
#     try:
#         database_data = fetch_data_from_db()
#         category = load_category_from_yaml()  # Get category from agents.yaml

#         if not database_data:
#             print("⚠️ No data retrieved from the database. Using default topic.")
#             topic_from_db = category  # Use category from YAML
#         else:
#             topic_from_db = database_data[0][0]  # Get first field from DB

#         inputs = {
#             'topic': topic_from_db,
#             'current_year': str(datetime.now().year)
#         }

#         print(f"🔹 Running Crew with inputs: {inputs}")
#         CrewAi().crew().kickoff(inputs=inputs)

#     except Exception as e:
#         print(f"❌ Error: {e}")
#         raise Exception(f"An error occurred while running the crew: {e}")

def run():
    """
    Run the crew (fetch database data only).
    """
    try:
        database_data = fetch_data_from_db()

        if not database_data:
            print("⚠️ No data retrieved from the database.")
        else:
            print("📌 Retrieved Data from Database:")
            for row in database_data:
                print(row)

    except Exception as e:
        print(f"❌ Error: {e}")
        raise Exception(f"An error occurred while fetching database data: {e}")



def train():
    """
    Train the crew for a given number of iterations.
    """
    try:
        CrewAi().crew().train(
            n_iterations=int(sys.argv[1]),
            filename=sys.argv[2],
            inputs={"topic": "AI LLMs"}
        )
    except Exception as e:
        raise Exception(f"An error occurred while training the crew: {e}")

def replay():
    """
    Replay the crew execution from a specific task.
    """
    try:
        CrewAi().crew().replay(task_id=sys.argv[1])
    except Exception as e:
        raise Exception(f"An error occurred while replaying the crew: {e}")

def test():
    """
    Test the crew execution and return results.
    """
    try:
        CrewAi().crew().test(
            n_iterations=int(sys.argv[1]),
            openai_model_name=sys.argv[2],
            inputs={"topic": "AI LLMs"}
        )
    except Exception as e:
        raise Exception(f"An error occurred while testing the crew: {e}")
