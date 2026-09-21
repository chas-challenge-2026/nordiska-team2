export type FaqEntry = {
    id: string;
    question: string;
    answer: string;
    category: string;
    keywords: string[];
};

export type FaqCategory = {
    id: string;
    label: string;
    icon: string;
    questionCount: number;
};