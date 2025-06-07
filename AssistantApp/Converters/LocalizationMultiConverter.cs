using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace AssistantApp.Converters
{
    public class LocalizationMultiConverter : IMultiValueConverter
    {
        private static readonly Dictionary<string, (string Ru, string En)> Translations = new()
        {
            { "Chest pain",                       ("Боль в груди",                               "Chest pain") },
            { "Abdominal pain",                   ("Боль в животе",                              "Abdominal pain") },
            { "Joint pain",                       ("Боль в суставах",                            "Joint pain") },
            { "Back pain",                        ("Боль в спине",                               "Back pain") },
            { "Nausea",                           ("Тошнота",                                    "Nausea") },
            { "Diarrhea",                         ("Диарея",                                     "Diarrhea") },
            { "Vomiting",                         ("Рвота",                                      "Vomiting") },
            { "Loss of appetite",                 ("Потеря аппетита",                            "Loss of appetite") },
            { "Dizziness",                        ("Головокружение",                             "Dizziness") },
            { "Palpitations",                     ("Сердцебиение",                               "Palpitations") },
            { "Headache",                         ("Головная боль",                              "Headache") },
            { "Muscle weakness",                  ("Слабость мышц",                              "Muscle weakness") },
            { "Blurred vision",                   ("Затуманенное зрение",                        "Blurred vision") },
            { "Fatigue",                          ("Усталость",                                  "Fatigue") },
            { "Sleepiness",                       ("Сонливость",                                 "Sleepiness") },
            { "Fever",                            ("Лихорадка",                                  "Fever") },
            { "Cough",                            ("Кашель",                                     "Cough") },
            { "Shortness of breath",              ("Одышка",                                     "Shortness of breath") },
            { "Rash",                             ("Сыпь",                                       "Rash") },
            { "Swelling",                         ("Отёк",                                       "Swelling") },
            { "Itching",                          ("Зуд",                                        "Itching") },
            { "Chills",                           ("Озноб",                                      "Chills") },
            { "Sore throat",                      ("Боль в горле",                               "Sore throat") },
            { "Weight loss",                      ("Похудение",                                  "Weight loss") },
            { "Excessive thirst",                 ("Чрезмерная жажда",                           "Excessive thirst") },

            { "Gripp",                            ("Грипп",                                      "Flu") },
            { "Pnevmoniya",                       ("Пневмония",                                  "Pneumonia") },
            { "KOVID-19",                         ("COVID-19",                                   "COVID-19") },
            { "Astma",                            ("Астма",                                      "Asthma") },
            { "Hronicheskaya obstruktivnaya bolezn legkih", ("ХОБЛ",                  "COPD") },
            { "Saharnyj diabet",                  ("Сахарный диабет",                            "Diabetes mellitus") },
            { "Arterialnaya gipertenziya",        ("Артериальная гипертензия",                   "Hypertension") },
            { "Infarkt miokarda",                 ("Инфаркт миокарда",                           "Myocardial infarction") },
            { "Insult",                           ("Инсульт",                                    "Stroke") },
            { "Tuberkulez",                       ("Туберкулёз",                                 "Tuberculosis") },
            { "Yazvennaya bolezn zheludka",       ("Язвенная болезнь желудка",                   "Peptic ulcer disease") },
            { "Gepatit B",                        ("Гепатит B",                                  "Hepatitis B") },
            { "Pochechnaya nedostatochnost",      ("Почечная недостаточность",                   "Renal failure") },
            { "Anemiya",                          ("Анемия",                                     "Anemia") },
            { "Artroz",                           ("Артроз",                                     "Osteoarthritis") },
            { "Revmatoidnyj artrit",              ("Ревматоидный артрит",                        "Rheumatoid arthritis") },
            { "Migren",                           ("Мигрень",                                    "Migraine") },
            { "Epilepsiya",                       ("Эпилепсия",                                  "Epilepsy") },
            { "Depressiya",                       ("Депрессия",                                  "Depression") },
            { "Shizofreniya",                     ("Шизофрения",                                 "Schizophrenia") },
            { "Alcgejmer bolezn",                 ("Болезнь Альцгеймера",                        "Alzheimer’s disease") },
            { "VICH-infekciya",                   ("ВИЧ-инфекция",                               "HIV infection") },
            { "Appendicit",                       ("Аппендицит",                                 "Appendicitis") },
            { "Holelitiaz",                       ("Желчнокаменная болезнь",                     "Cholelithiasis") },
            { "Holecistit",                       ("Холецистит",                                 "Cholecystitis") },
            { "Cirroz pecheni",                   ("Цирроз печени",                              "Liver cirrhosis") },
            { "Tireotoksikoz",                    ("Тиреотоксикоз",                              "Thyrotoxicosis") },
            { "Rak legkogo",                      ("Рак лёгкого",                                "Lung cancer") },
            { "Rak molochnoj zhelezy",            ("Рак молочной железы",                        "Breast cancer") },
            { "Rak predstatelnoj zhelezy",        ("Рак предстательной железы",                  "Prostate cancer") },

            { "Bol",                                           ("Боль",                                     "Pain") },
            { "Simptomy gastroenterologicheskikh zabolevaniy", ("Симптомы гастроэнтерологических заболеваний", "Gastrointestinal symptoms") },
            { "Golovokruzhenie",                               ("Головокружение",                           "Dizziness") },
            { "Narusheniya ritma serdtsa",                     ("Нарушения ритма сердца",                   "Heart rhythm disorders") },
            { "Simptomy zabolevaniy nervnoy sistemy",           ("Симптомы заболеваний нервной системы",      "Neurological symptoms") },
            { "Psikhicheskie ili povedencheskie simptomy, priznaki ili klinicheskie dannye",
                                                               ("Психические или поведенческие симптомы",    "Psychiatric or behavioral symptoms") },
            { "Simptomy psikhicheskikh rasstroystv",            ("Симптомы психических расстройств",         "Symptoms of psychiatric disorders") },
            { "Radiologicheskie simptomy",                     ("Радиологические симптомы",                 "Radiological symptoms") },
            { "Urologicheskie simptomy",                       ("Урологические симптомы",                   "Urological symptoms") },
            { "Simptomy khirurgicheskikh bolezney",            ("Симптомы хирургических болезней",          "Surgical symptoms") },
            { "Simptomy endokrinykh zabolevaniy",               ("Симптомы эндокринных заболеваний",         "Endocrine symptoms") }
        };

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var key = values[0] as string;
            var lang = values[1] as string;
            if (key == null || lang == null) return key;
            if (Translations.TryGetValue(key, out var t))
                return lang == "Русский" ? t.Ru : t.En;
            return key;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
