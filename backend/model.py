import pandas as pd
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import linear_kernel
import heapq

class Model:

    def __init__(self) -> None:
        pd.set_option('display.max_colwidth', None)
        df = pd.read_json("./descriptions.json")

        self.paths = df["annotations"].apply(lambda x: x["image_path"])
        self.artstyles = df["annotations"].apply(lambda x: x["artstyle"])
        self.themes = df["annotations"].apply(lambda x: x["theme"])
        self.colors = df["annotations"].apply(lambda x:  " ".join(x["colors"]))
        self.semantics = df["annotations"].apply(lambda x: " ".join(x["semantics"]))
        self.objects = df["annotations"].apply(lambda x: " ".join(x["objects"]))
        self.descriptions = df["annotations"].apply(lambda x: x["description"])

    def __create_sim_scores(self, features):
        tfidf = TfidfVectorizer(stop_words="english")
        tfidf_matrix = tfidf.fit_transform(features)
        cosine_sim = linear_kernel(tfidf_matrix, tfidf_matrix)
        sim_scores = cosine_sim[0][1:]
        return sim_scores
    
    def delete_word(self, sentence, word):
        import re
        # Regular expression to find whole words, handling punctuation and case insensitivity
        pattern = r'\b' + re.escape(word) + r'\b'
        # Replace the found word with nothing
        cleaned_sentence = re.sub(pattern, '', sentence, flags=re.IGNORECASE).strip()
        # Clean up any excess whitespace that might be left
        cleaned_sentence = re.sub(r'\s{2,}', ' ', cleaned_sentence)
        return cleaned_sentence

    def __combine_categories(self, category_list, image_names):
        accum = ""

        for i in image_names:
            image_index = self.paths[self.paths == i].index
            sentence = category_list.iloc[image_index].iloc[0]
            accum = accum + " " + sentence
        
        return accum
    
    def __create_features_matrix_from_category(self, image_names, category_list):
        return [self.__combine_categories(category_list, image_names)] + category_list.tolist()

    def __compute_description_similarity(self, image_names):
        feature_matrix = self.__create_features_matrix_from_category(image_names, self.descriptions)
        return self.__create_sim_scores(feature_matrix)
    
    def __compute_artstyle_similarity(self, image_names):
        feature_matrix = self.__create_features_matrix_from_category(image_names, self.artstyles)
        return self.__create_sim_scores(feature_matrix)
    
    def __compute_theme_similarity(self, image_names):
        feature_matrix = self.__create_features_matrix_from_category(image_names, self.themes)
        return self.__create_sim_scores(feature_matrix) / 2

    def __compute_color_similarity(self, image_names):
        colors_count = 0
        for i in image_names:
            image_index = self.paths[self.paths == i].index
            colors_count += len(self.colors.iloc[image_index])

        feature_matrix = self.__create_features_matrix_from_category(image_names, self.colors)
        return self.__create_sim_scores(feature_matrix) / colors_count
    
    def __compute_semantics_similarity(self, image_names):
        feature_matrix = self.__create_features_matrix_from_category(image_names, self.semantics)
        return self.__create_sim_scores(feature_matrix) 
    
    def __compute_objects_similarity(self, image_names):
        feature_matrix = self.__create_features_matrix_from_category(image_names, self.objects)
        return self.__create_sim_scores(feature_matrix) 
        
        
    def get_similar(self, image_names, exclude_images, k=3):

        sim_scores = 0

        sim_scores += self.__compute_objects_similarity(image_names)
        sim_scores += self.__compute_color_similarity(image_names)
        sim_scores += self.__compute_semantics_similarity(image_names)
        sim_scores += self.__compute_theme_similarity(image_names)
        sim_scores += self.__compute_artstyle_similarity(image_names)
        sim_scores += self.__compute_description_similarity(image_names)

        def get_similarity_score(idx):
            return 0 if (self.paths[idx] in image_names or self.paths[idx] in exclude_images)  else sim_scores[idx] 
        
        indices = heapq.nlargest(k, range(len(sim_scores)), key=get_similarity_score)
        return indices

    def get_recommendations(self, image_names, exclude_images, k=2):
        indices = self.get_similar(image_names, exclude_images , k)
        recommendations = []
        for i in indices:
            explanatioon = self.get_explanation(i, image_names)
            recommendations.append({self.paths[i] : explanatioon })

        return recommendations

    def get_explanation(self, recommended, favorites = []):
        recommended_index = recommended

        objects_sim = 0
        color_sim = 0
        semantics_sim = 0
        theme_sim = 0
        artstyle_sim = 0
        description_sim = 0

        objects_sim = self.__compute_objects_similarity(favorites)[recommended_index]
        color_sim =  self.__compute_color_similarity(favorites)[recommended_index]
        semantics_sim = self.__compute_semantics_similarity(favorites)[recommended_index]
        theme_sim = self.__compute_theme_similarity(favorites)[recommended_index]
        artstyle_sim = self.__compute_artstyle_similarity(favorites)[recommended_index]
        description_sim = self.__compute_description_similarity(favorites)[recommended_index]

        total = objects_sim + color_sim + semantics_sim + description_sim + theme_sim + artstyle_sim

        sim_scores = {
            "objects": {"importance" : objects_sim / total, "factors" : self.__get_explanation_objects(recommended, favorites)},
            "colors": {"importance" :color_sim / total, "factors" :  self.__get_explanation_colors(recommended, favorites)},
            "semantics": {"importance" :semantics_sim / total, "factors" : self.__get_explanation_semantics(recommended, favorites)},
            "description":{"importance" :description_sim / total, "factors" :  self.__get_explanation_description(recommended, favorites)},
            "theme": {"importance" :theme_sim / total, "factors" : self.__get_explanation_theme(recommended, favorites)},
            "artstyle": {"importance" :artstyle_sim / total, "factors" :  self.__get_explanation_artstyle(recommended, favorites)}
        }
        return sim_scores
    
    def get_explanation_description_of(self, recommended_soup, favorites_soup):
        tfidf = TfidfVectorizer(stop_words="english")
        tfidf.fit_transform(self.descriptions.tolist())
        recommended_vector = tfidf.transform([recommended_soup])
        favorites_vector = tfidf.transform([favorites_soup])

        recommended_words = {word: val for word, val in zip(tfidf.get_feature_names_out(), recommended_vector.toarray()[0]) if val > 0}
        favorites_words = {word: val for word, val in zip(tfidf.get_feature_names_out(), favorites_vector.toarray()[0]) if val > 0}

        common_words = set(recommended_words.keys()) & set(favorites_words.keys())

        explanation = {word: (recommended_words[word], favorites_words[word]) for word in common_words}
        sorted_words = sorted(explanation.items(), key=lambda x: (x[1][0] + x[1][1]) / 2, reverse=True)

        top_10_words = {word: sum(values) for word, values in sorted_words[:10]}

        return top_10_words
    
    def __get_explanation_description(self, recommended, favorites):
        recommended_index = recommended
        recommended_soup = str(self.descriptions.iloc[recommended_index])
        favorites_soup = self.__combine_categories(self.descriptions, favorites)
        return self.get_explanation_description_of(recommended_soup, favorites_soup)
    
    def __get_explanation_colors(self, recommended, favorites):
        recommended_index = recommended
        recommended_soup = str(self.colors.iloc[recommended_index])
        favorites_soup = self.__combine_categories(self.colors, favorites)
        return self.get_explanation_description_of(recommended_soup, favorites_soup)
        
    def __get_explanation_semantics(self, recommended, favorites):
        recommended_index = recommended
        recommended_soup = str(self.semantics.iloc[recommended_index])
        favorites_soup = self.__combine_categories(self.semantics, favorites)
        return self.get_explanation_description_of(recommended_soup, favorites_soup)
    

    def __get_explanation_objects(self, recommended, favorites):
        recommended_index = recommended
        recommended_soup = str(self.objects.iloc[recommended_index])
        favorites_soup = self.__combine_categories(self.objects, favorites)
        return self.get_explanation_description_of(recommended_soup, favorites_soup)
    
    def __get_explanation_theme(self, recommended, favorites):
        recommended_index = recommended
        recommended_soup = str(self.themes.iloc[recommended_index])
        favorites_soup = self.__combine_categories(self.themes, favorites)
        return self.get_explanation_description_of(recommended_soup, favorites_soup)
    
    def __get_explanation_artstyle(self, recommended, favorites):
        recommended_index = recommended
        recommended_soup = str(self.artstyles.iloc[recommended_index])
        favorites_soup = self.__combine_categories(self.artstyles, favorites)
        return self.get_explanation_description_of(recommended_soup, favorites_soup)        