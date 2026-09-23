export function LoadingRows({ label }: { label: string }) {
  return (
    <div className="panel panel-flush" aria-busy="true">
      <p className="visually-hidden">{label}</p>
      {[0, 1, 2].map((row) => (
        <div key={row} className="skeleton-row" aria-hidden="true">
          <span className="skeleton skeleton-wide" />
          <span className="skeleton" />
        </div>
      ))}
    </div>
  )
}
