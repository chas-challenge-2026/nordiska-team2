type PageHeaderProps= {
    title: string;
    subtitle?:string;
}


export default function PageHeader({title, subtitle} : PageHeaderProps ) {
    return(
        <header className="flex flex-col gap-1.5 p-4">
            <h1 className="text-page-title font-medium text-foreground">{title}</h1>
                {subtitle && <p className="text-small text-muted">{subtitle}</p>}
        </header>
    )
}