# from fastapi import FastAPI
# from pydantic import BaseModel
# import os
# from crewai import Agent, Task, Crew
# import litellm

# app = FastAPI()

# litellm.set_verbose=True
# # Set API Key
# os.environ["OPENAI_API_KEY"] = "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"

# # LiteLLM configuration
# litellm.api_key = os.getenv("OPENAI_API_KEY")
# litellm.base_url = "https://api.groq.com/openai/v1"

# class InputData(BaseModel):
#     topic: str
#     current_year: str

# # Define Agents
# agent1 = Agent(
#     name="Researcher",
#     role="AI Researcher",
#     goal="Provide detailed insights on AI advancements.",
#     backstory="Expert in AI and machine learning."
# )

# agent2 = Agent(
#     name="Analyst",
#     role="Market Analyst",
#     goal="Analyze the impact of AI advancements on industries.",
#     backstory="Experience in business and market research."
# )

# # Define Tasks
# task1 = Task(
#     description="Research and summarize the latest AI advancements.",
#     agent=agent1,
#     expected_output="A detailed summary of the latest AI advancements."
# )

# task2 = Task(
#     description="Analyze the market trends influenced by AI advancements.",
#     agent=agent2,
#     expected_output="A market trend analysis report on AI impact."
# )

# @app.post("/run-crew")
# async def run_crew(data: InputData):
#     try:
#         # Create Crew with LiteLLM as the client
#         crew = Crew(agents=[agent1, agent2], tasks=[task1, task2], client=litellm)

#         # Run the CrewAI workflow
#         results = crew.kickoff(inputs={"topic": data.topic, "current_year": data.current_year})

#         return {
#             "message": f"Crew executed for topic: {data.topic} and year: {data.current_year}",
#             "results": results
#         }
#     except Exception as e:
#         return {"error": str(e)}

import re
from fastapi import FastAPI, Depends, HTTPException
from pydantic import BaseModel
import os
import litellm
from crewai import Agent, Task, Crew
from sqlalchemy import create_engine, Column, Integer, String, func, text
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, Session
from sqlalchemy.sql import text
from dotenv import load_dotenv
import urllib
from pathlib import Path

env_path = Path(__file__).resolve().parent / 'RestApi' / 'fastapi_service' / '.env'
load_dotenv(dotenv_path=env_path)

# load_dotenv()

app = FastAPI()


GROQ_API_KEY = os.getenv("GROQ_API_KEY", "")  #add while debugging
GROQ_BASE_URL = os.getenv("GROQ_BASE_URL", "https://api.groq.com/openai/v1") #add while debugging
os.environ["LITELLM_LOG"] = "DEBUG"
DB_SERVER = os.getenv("DB_SERVER")
DB_NAME = os.getenv("DB_NAME")
DB_USER = os.getenv("DB_USER")
DB_PASSWORD = os.getenv("DB_PASSWORD")

params = urllib.parse.quote_plus(
    f"DRIVER={{ODBC Driver 17 for SQL Server}};"
    f"SERVER={DB_SERVER};"
    f"DATABASE={DB_NAME};"
    f"UID={DB_USER};"
    f"PWD={DB_PASSWORD};"
    "Encrypt=yes;"
    "TrustServerCertificate=yes;"
    "Connection Timeout=30;"
)

DATABASE_URL = f"mssql+pyodbc:///?odbc_connect={params}"

try:
    engine = create_engine(DATABASE_URL)
    connection = engine.connect()
    print("✅ SQLAlchemy Connection successful!")
    connection.close()
except Exception as e:
    print("❌ SQLAlchemy Connection failed:", e)

engine = create_engine(DATABASE_URL, echo=True)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()

# class ResearchResult(Base):
#     __tablename__ = "Users"
#     userId = Column(Integer, primary_key=True, index=True)
#     roleId = Column(Integer)

class User:
    __tablename__ = 'Users'
    userId = Column(Integer, primary_key=True, index=True)
    roleId = Column(Integer)
    
def init_db():
    Base.metadata.create_all(bind=engine)

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

class QueryInput(BaseModel):
    query: str

researcher_agent = Agent(
    name="Researcher",
    role="AI Researcher",
    goal="Provide insights on AI advancements.",
    backstory="Expert in AI and machine learning."
)

analyst_agent = Agent(
    name="Market Analyst",
    role="Market Researcher",
    goal="Analyze AI trends' impact on industries.",
    backstory="Experienced in business analysis."
)

task1 = Task(
    description="Research the latest AI advancements.",
    agent=researcher_agent,
    expected_output="Detailed AI advancements summary."
)


task2 = Task(
    description="Analyze AI impact on industry trends.",
    agent=analyst_agent,
    expected_output="Market trend report on AI."
)
#  model="llama-3.3-70b-versatile",

# Function to extract role name from the query
def extract_role(query):
    if not query:
        return None
    query_lower = query.lower()
    if "role id" in query_lower:
        return query.split("role id ")[-1].strip("? ")
    elif "with the role" in query_lower:
        return query.split("with the role ")[-1].strip("? ")
    elif "role" in query_lower:
        return query.split("role ")[-1].strip("? ")
    return None

# Function to generate SQL query
def generate_sql_query(query, db):
    if not query:
        raise ValueError("Query is empty or invalid.")

    # Extract role name
    role = extract_role(query)

    # Map role name to roleId (replace this with your actual mapping logic)
    role_mapping = {
        "admin": 2,
        "user": 3,
        "manager": 4
    }
    roleId = role_mapping.get(role.lower(), None) if role else None  # Default to None if role not found

    # Construct the prompt for the LLM
    if roleId is not None:
        prompt = f"Convert this natural language query to a SQL query: {query}. Use roleId = {roleId} in the SQL query."
    else:
        prompt = f"Convert this natural language query to a SQL query: {query}"

    # Call the LLM to generate the SQL query
    response = litellm.completion(
        model="llama-3.3-70b-versatile",
        messages=[
            {"role": "system", "content": "You are a helpful assistant that converts user queries into SQL queries."},
            {"role": "user", "content": prompt}
        ],
        api_key=GROQ_API_KEY,
        base_url=GROQ_BASE_URL,
        temperature=0.7,
        max_tokens=150
    )

    # Extract the generated SQL query from the response
    structured_query = response["choices"][0]["message"]["content"]
    print(f"Structured query: {structured_query}")

    # Extract SQL query from the structured response
    sql_query = re.search(r"```sql\s*(.*?)\s*```", structured_query, re.DOTALL)
    if sql_query:
        sql_query = sql_query.group(1).strip()
        sql_query = sql_query.replace("\n", "")  # Remove newlines
    else:
        sql_query = structured_query.strip()

    if not sql_query:
        raise ValueError("Generated SQL query is empty or invalid.")

    print(f"Generated SQL Query: {sql_query}")

    # Execute the SQL query
    result = db.execute(text(sql_query)).fetchall()
    # result_text = f"Query Result: {result}"
    # return {"message": result_text}
    
    result_prompt = f"The SQL query `{sql_query}` returned the following result: {result}. Summarize this result in natural language."
    result_response = litellm.completion(
        model="llama-3.3-70b-versatile",
        messages=[
            {"role": "system", "content": "You are a helpful assistant that summarizes SQL query results in natural language."},
            {"role": "user", "content": result_prompt}
        ],
        api_key=GROQ_API_KEY,
        base_url=GROQ_BASE_URL,
        temperature=0.7,
        max_tokens=150
    )

    # Extract the natural language response
    result_text = result_response["choices"][0]["message"]["content"]
    return {"message": result_text}

@app.post("/run-crew")
async def run_crew(data: QueryInput, db: Session = Depends(get_db)):
    try:
        # Validate input
        if not data.query:
            raise HTTPException(status_code=400, detail="Query field is missing or empty.")

        query = data.query.lower()

        if not GROQ_API_KEY:
            return {"error": "API key is missing. Set 'GROQ_API_KEY'."}

        # Call the generate_sql_query function
        return generate_sql_query(query, db)

    except HTTPException as e:
        raise e
    except Exception as e:
        return {"error": str(e)}

        # crew = Crew(agents=[researcher_agent, analyst_agent], tasks=[task1, task2])
        # results = []
        # for task in crew.tasks:
        #     response = litellm.completion(
        #         model="llama-3.3-70b-versatile",
        #         messages=[ 
        #             {"role": "system", "content": f"You are {task.agent.role}."},
        #             {"role": "user", "content": f"{task.description} for {data.topic} in {data.current_year}."}
        #         ],
        #         api_key=GROQ_API_KEY,
        #         base_url=GROQ_BASE_URL,
        #         temperature=0.7,
        #         max_tokens=500
        #     )

        #     result_text = response["choices"][0]["message"]["content"]

        #     new_result = ResearchResult(
        #         topic=data.topic,
        #         year=data.current_year,
        #         result=result_text
        #     )
        #     db.add(new_result)
        #     db.commit()
        #     db.refresh(new_result)

        #     results.append({
        #         "task": task.description,
        #         "agent_role": task.agent.role,
        #         "result": result_text
        #     })

        # return {
        #     # "message": f"Crew executed for {data.topic} in {data.current_year}",
        #     # "results": results,
        #     "message": result_text
        # }

    # except Exception as e:
    #     return {"error": str(e)}