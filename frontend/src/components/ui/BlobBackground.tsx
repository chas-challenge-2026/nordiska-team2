// Mjuka, suddiga färgfläckar bakom innehållet. Syns bara i ljust läge.
// Föräldern behöver klasserna "relative isolate" så att fläckarna hamnar bakom innehållet.
export default function BlobBackground() {
    return (
        <div aria-hidden="true"
             className="pointer-events-none absolute inset-0 -z-10 overflow-hidden bg-white dark:hidden">
            <div className="absolute -top-24 -left-20 size-96 rounded-full bg-sky-200 blur-3xl opacity-70" />
            <div className="absolute top-1/3 -right-24 size-80 rounded-full bg-brand blur-3xl opacity-15" />
            <div className="absolute -bottom-24 left-1/3 size-96 rounded-full bg-blue-200 blur-3xl opacity-50" />
        </div>
    );
}
