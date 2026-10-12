// Mjuka, suddiga färgfläckar bakom innehållet.
// Ljust läge: vit bakgrund med tydliga blå fläckar.
// Mörkt läge: samma fläckar men mycket svagare, så att bakgrunden bara mjuknar lite.
// Föräldern behöver klasserna "relative isolate" så att fläckarna hamnar bakom innehållet.
export default function BlobBackground() {
    return (
        <div aria-hidden="true"
             className="pointer-events-none absolute inset-0 -z-10 overflow-hidden bg-white dark:bg-background">
            <div className="absolute -top-24 -left-20 size-96 rounded-full blur-3xl
                            bg-sky-200 opacity-70 dark:bg-sky-500 dark:opacity-10" />
            <div className="absolute top-1/3 -right-24 size-80 rounded-full blur-3xl
                            bg-brand opacity-15 dark:bg-brand dark:opacity-30" />
            <div className="absolute -bottom-24 left-1/3 size-96 rounded-full blur-3xl
                            bg-blue-200 opacity-50 dark:bg-blue-600 dark:opacity-10" />
        </div>
    );
}
