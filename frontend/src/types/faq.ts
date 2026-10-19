export type FaqEntry = {
    id: number;
    question: string;
    answer: string;
    category: string;

};

export type FaqCategory = {
    id: string;
    label: string;
    questionCount: number;
};

export type FaqSearchResult = {
    matched: boolean;
    question: string | null;
    answer: string;
    category: string | null;
}