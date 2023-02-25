namespace Atmo.Helpers;

/// <summary>
/// A composable UAD subclass.
/// </summary>
public class EventfulUAD : UpdatableAndDeletable, IDrawable {
	/// <summary>
	/// This is used to distinguish eventfuls between each other.
	/// </summary>
	public Guid id = Guid.NewGuid();
	/// <summary>
	/// Code that should run every update.
	/// </summary>
	public Action<bool>? onUpdate;
	/// <summary>
	/// Code that should run on paused updates.
	/// </summary>
	public Action? onPausedUpdate;
	/// <summary>
	/// Code that should run on destruct.
	/// </summary>
	public Action? onDestroy;
	/// <summary>
	/// Code that should run on IDrawable.InitiateSprites.
	/// </summary>
	public Action<RoomCamera.SpriteLeaser, RoomCamera>? onInitSprites;
	/// <summary>
	/// Code that should run on IDrawable.DrawSprites.
	/// </summary>
	public Action<RoomCamera.SpriteLeaser, RoomCamera, float, Vector2>? onDraw;
	/// <summary>
	/// Code that should run on IDrawable.ApplyPalette.
	/// </summary>
	public Action<RoomCamera.SpriteLeaser, RoomCamera, RoomPalette>? onApplyPalette;
	/// <summary>
	/// Code that should run on IDrawable.AddToContainer.
	/// </summary>
	public Action<RoomCamera.SpriteLeaser, RoomCamera, FContainer?>? onAddToContainer;
	/// <summary>
	/// Additional data bundled with the object.
	/// </summary>
	public readonly Dictionary<string, object?> data = new();
	/// <summary>
	/// Performs a lookup in the Data field.
	/// </summary>
	public object? this[string field] {
		get => data.EnsureAndGet(field, () => null);
		set { data[field] = value; }
	}
	/// <inheritdoc/>
	public override void Update(bool eu) {
		base.Update(eu);
		try {
			onUpdate?.Invoke(eu);
		}
		catch (Exception ex) {
			__ReportError(this, Update, ex);
		}
	}
	/// <inheritdoc/>
	public override void PausedUpdate() {
		base.PausedUpdate();
		try {
			onPausedUpdate?.Invoke();
		}
		catch (Exception ex) {
			__ReportError(this, PausedUpdate, ex);
		}
	}
	/// <inheritdoc/>
	public override void Destroy() {
		base.Destroy();
		try {
			onDestroy?.Invoke();
		}
		catch (Exception ex) {
			__ReportError(this, Destroy, ex);
		}
	}
	/// <inheritdoc/>
	public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
		//if (onInitSprites is null) return;
		try {
			onInitSprites?.Invoke(sLeaser, rCam);
		}
		catch (Exception ex) {
			__ReportError(this, InitiateSprites, ex);
		}
		finally {
			sLeaser.sprites ??= new FSprite[0];
		}
	}
	/// <inheritdoc/>
	public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
		if (onDraw is null) return;
		try {
			onDraw?.Invoke(sLeaser, rCam, timeStacker, camPos);
		}
		catch (Exception ex) {
			__ReportError(this, DrawSprites, ex);
		}
	}
	/// <inheritdoc/>
	public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
		try {
			onApplyPalette?.Invoke(sLeaser, rCam, palette);
		}
		catch (Exception ex) {
			__ReportError(this, ApplyPalette, ex);
		}
	}
	/// <inheritdoc/>
	public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer? newContatiner) {
		try {
			onAddToContainer?.Invoke(sLeaser, rCam, newContatiner);
		}
		catch (Exception ex) {
			__ReportError(this, PausedUpdate, ex);
		}
	}

	private static void __ReportError(EventfulUAD uad, Delegate where, object? err) {
		__logger.LogError($"EventfulUAD {uad.id}: error on {where.Method.Name}: {err}");
	}
	/// <inheritdoc/>
	public class Extra<T> : EventfulUAD{
		/// <summary>
		/// First extra item.
		/// </summary>
		public T? _0;
	}
	/// <inheritdoc/>
	public class Extra<T1, T2> : EventfulUAD{
		/// <summary>
		/// First extra item.
		/// </summary>
		public T1? _0;
		/// <summary>
		/// Second extra item.
		/// </summary>
		public T2? _1;
	}
	/// <inheritdoc/>
	public class Extra<T1, T2, T3> : EventfulUAD{
		/// <summary>
		/// First extra item.
		/// </summary>
		public T1? _0;
		/// <summary>
		/// Second extra item.
		/// </summary>
		public T2? _1;
		/// <summary>
		/// Third extra item.
		/// </summary>
		public T3? _2;
	}
}
