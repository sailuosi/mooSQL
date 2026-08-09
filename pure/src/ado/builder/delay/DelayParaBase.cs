namespace mooSQL.data
{
    /// <summary><see cref="IDelayPara"/> 公共：PlaceHolder / Owner 绑定。</summary>
    public abstract class DelayParaBase : IDelayPara
    {
        private string _placeHolder;

        /// <inheritdoc />
        public string PlaceHolder { get { return _placeHolder; } }

        /// <summary>所属 Paras；Copy 后由 <see cref="BindOwner"/> 更新。</summary>
        protected Paras Owner { get; private set; }

        /// <inheritdoc />
        public void BindPlaceHolder(int delayParaIndex)
        {
            _placeHolder = LiveParaMarks.Format(delayParaIndex);
        }

        /// <inheritdoc />
        public void BindOwner(Paras owner)
        {
            Owner = owner;
        }

        /// <inheritdoc />
        public string Run()
        {
            return RunCore();
        }

        /// <summary>子类实现具体拼装。</summary>
        protected abstract string RunCore();
    }
}
