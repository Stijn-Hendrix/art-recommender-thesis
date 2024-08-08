from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import FileResponse
from typing import List
from pydantic import BaseModel
import random
from itertools import permutations

from model import Model

app = FastAPI()

# Allow all origins in a real deployment, you may want to restrict this to a specific domain or a list of allowed origins.
origins = ["*"]

app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

class RecommendationRequest(BaseModel):
    image_names: List[str]
    condition : int

model = Model()

categories = ["objects", "colors", "semantics", "theme", "artstyle", "description"]
per_category = 3
amount_recs_needed = len(categories) * per_category + per_category 

full_list_permutations = list(permutations(categories))
categories_conditions = list(full_list_permutations)  # Copy the list to shuffle
random.shuffle(categories_conditions)

query_cache = {}

def images_from_rec(rec):
    return [list(img_path.keys())[0] for img_path in rec]

@app.post("/recommendation/")
def read_recommendation(recreq : RecommendationRequest):

    query = ', '.join(recreq.image_names)

    print(f"Recommendation request for: {', '.join(recreq.image_names)}, condition: {recreq.condition}")

    if query in query_cache:
        print("Return cached")
        print(query_cache[query])
        return query_cache[query]

 
    result = []
    exclude = []
    excluded_recs = []


    # Initialize a few recommendations
    excluded_recs = model.get_recommendations(recreq.image_names, exclude, k = amount_recs_needed)
    for rec in excluded_recs:
        exclude.append(list(rec)[0])

    def rec_contains_category(rec, category):
        """ Check if rec contains category with importance > 0.0 """
        return rec[list(rec)[0]][category]["importance"] > 0.0

    def clear_except_category(rec, category):
        """ Set importances to 0 except for category """

        key = list(rec)[0]
        q = rec[key]
        for cat in q:
            if cat != category:
                q[cat] = {"importance" : 0.0, "factors" :  {}}
        return { key : q }

    def fill_c(category, per_category):
        """ Find per_category amount of recommendations that contain category """

        i = 0

        print("Generating " + category)

        # First check if the cached recs have a valid rec
        for rec in excluded_recs:
            if rec_contains_category(rec, category):
                result.append(clear_except_category(rec, category))
                excluded_recs.remove(rec)
                i += 1

            if i == per_category:
                return

        # Keep generating recs until a valid is found, always generate per_category amount, because
        # a single call to get_recommendation is cheaper than multiple calls
        infinite_guard = False
        while i < per_category and not infinite_guard:
            recs = model.get_recommendations(recreq.image_names, exclude, k = per_category)

            for rec in recs:
                rec_key = list(rec)[0]
                if rec_contains_category(rec, category):
                    result.append(clear_except_category(rec, category))
                    exclude.append(rec_key)

                    i += 1
                else:
                    excluded_recs.append(rec)
                    exclude.append(rec_key)

                if i == 4 * amount_recs_needed:
                    print("Infinite loop!")
                    infinite_guard = True # Break out of infinite loop

                if i == per_category:
                    return


    for cat in categories_conditions[recreq.condition]:
        fill_c(cat, per_category)

    result += model.get_recommendations(recreq.image_names, exclude, k = per_category)


    query_cache[query] = {'Recommendations' : result, 'Categories': categories_conditions[recreq.condition]}
    #print(combined)
    return {'Recommendations' : result, 'Categories': categories_conditions[recreq.condition]}
